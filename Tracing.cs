#region Copyright (c) 2000-2012 Developer Express Inc.
/*
{*******************************************************************}
{                                                                   }
{       Developer Express .NET Component Library                    }
{       eXpressApp Framework                                        }
{                                                                   }
{       Copyright (c) 2000-2012 Developer Express Inc.              }
{       ALL RIGHTS RESERVED                                         }
{                                                                   }
{   The entire contents of this file is protected by U.S. and       }
{   International Copyright Laws. Unauthorized reproduction,        }
{   reverse-engineering, and distribution of all or any portion of  }
{   the code contained in this file is strictly prohibited and may  }
{   result in severe civil and criminal penalties and will be       }
{   prosecuted to the maximum extent possible under the law.        }
{                                                                   }
{   RESTRICTIONS                                                    }
{                                                                   }
{   THIS SOURCE CODE AND ALL RESULTING INTERMEDIATE FILES           }
{   ARE CONFIDENTIAL AND PROPRIETARY TRADE                          }
{   SECRETS OF DEVELOPER EXPRESS INC. THE REGISTERED DEVELOPER IS   }
{   LICENSED TO DISTRIBUTE THE PRODUCT AND ALL ACCOMPANYING .NET    }
{   CONTROLS AS PART OF AN EXECUTABLE PROGRAM ONLY.                 }
{                                                                   }
{   THE SOURCE CODE CONTAINED WITHIN THIS FILE AND ALL RELATED      }
{   FILES OR ANY PORTION OF ITS CONTENTS SHALL AT NO TIME BE        }
{   COPIED, TRANSFERRED, SOLD, DISTRIBUTED, OR OTHERWISE MADE       }
{   AVAILABLE TO OTHER INDIVIDUALS WITHOUT EXPRESS WRITTEN CONSENT  }
{   AND PERMISSION FROM DEVELOPER EXPRESS INC.                      }
{                                                                   }
{   CONSULT THE END USER LICENSE AGREEMENT FOR INFORMATION ON       }
{   ADDITIONAL RESTRICTIONS.                                        }
{                                                                   }
{*******************************************************************}
*/
#endregion Copyright (c) 2000-2012 Developer Express Inc.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Security;
using System.Security.AccessControl;
using System.Security.Permissions;
using DevExpress.Utils;
using Microsoft.Win32;
namespace DevExpress.Persistent.Base 
{
    public class NeedContextInformationEventArgs : EventArgs
    {
        // Fields
        private string contextInformation = string.Empty;

        // Properties
        public string ContextInformation
        {
            get
            {
                return this.contextInformation;
            }
            set
            {
                this.contextInformation = value;
            }
        }
    }

    public class CustomFormatDateTimeStampEventArgs : EventArgs
    {
        // Methods
        public CustomFormatDateTimeStampEventArgs( DateTime dateTime, string result )
        {
            this.DateTime = dateTime;
            this.Result = result;
        }

        // Properties
        public DateTime DateTime
        {
            get;
            private set;
        }

        public string Result
        {
            get;
            set;
        }
    }

	public delegate void Method();

    [Serializable]
    public class DelayedException : Exception
    {
        // Fields
        private object targetObject;
        private string targetObjectIdentifier;

        // Methods
        protected DelayedException( SerializationInfo info, StreamingContext context )
            : base( info, context )
        {
            this.targetObjectIdentifier = info.GetString( "TargetObjectIdentifier" );
        }

        public DelayedException( Exception exception, object targetObject, string targetObjectIdentifier )
            : base( FormatMessage( exception.Message, targetObject, targetObjectIdentifier ), exception )
        {
            this.targetObject = targetObject;
            this.targetObjectIdentifier = targetObjectIdentifier;
        }

        public static string FormatMessage( string errorMessage, object targetObject, string targetObjectIdentifier )
        {
            string str = "";
            if ( targetObject != null )
            {
                str = "'" + targetObject.GetType() + "', ";
            }
            if ( !string.IsNullOrEmpty( targetObjectIdentifier ) )
            {
                str = str + "'" + targetObjectIdentifier + "'";
            }
            str = str.TrimEnd( new char[] { ',', ' ' } );
            if ( !string.IsNullOrEmpty( str ) )
            {
                return ( errorMessage + ". " + str );
            }
            return errorMessage;
        }

        public override void GetObjectData( SerializationInfo info, StreamingContext context )
        {
            Guard.ArgumentNotNull( info, "info" );
            info.AddValue( "TargetObjectIdentifier", this.targetObjectIdentifier );
            base.GetObjectData( info, context );
        }

        // Properties
        public object TargetObject
        {
            get
            {
                return this.targetObject;
            }
        }

        public string TargetObjectIdentifier
        {
            get
            {
                return this.targetObjectIdentifier;
            }
        }
    }

    public class SafeExecutor
    {
        // Fields
        private List<DelayedException> exceptionEntries;
        private object targetObject;
        private string targetObjectIdentifier;

        // Methods
        public SafeExecutor( object targetObject )
            : this( targetObject, "" )
        {
        }

        public SafeExecutor( object targetObject, string targetObjectIdentifier )
        {
            this.exceptionEntries = new List<DelayedException>();
            this.targetObject = targetObject;
            this.targetObjectIdentifier = targetObjectIdentifier;
        }

        public void Dispose( IDisposable targetObject )
        {
            this.Dispose( targetObject, "" );
        }

        public void Dispose( IDisposable targetObject, string targetObjectIdentifier )
        {
            this.Execute( () => targetObject.Dispose(), targetObject, targetObjectIdentifier );
        }

        public void Execute( Method method )
        {
            this.Execute( method, null, null );
        }

        public void Execute( Method method, object targetObject, string targetObjectIdentifier )
        {
            try
            {
                method();
            }
            catch ( Exception exception )
            {
                if ( Debugger.IsAttached )
                {
                    throw;
                }
                this.exceptionEntries.Add( new DelayedException( exception, targetObject, targetObjectIdentifier ) );
            }
        }

        public void ThrowExceptionIfAny()
        {
            if ( this.exceptionEntries.Count > 0 )
            {
                throw new DelayedExceptionList( this.exceptionEntries, this.targetObject, this.targetObjectIdentifier );
            }
        }

        public static bool TryExecute( Method method )
        {
            try
            {
                method();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Properties
        public List<DelayedException> Exceptions
        {
            get
            {
                return this.exceptionEntries;
            }
        }
    }

    [Serializable]
    public class DelayedExceptionList : Exception
    {
        // Fields
        private List<DelayedException> exceptions;

        // Methods
        protected DelayedExceptionList( SerializationInfo info, StreamingContext context )
            : base( info, context )
        {
            this.exceptions = info.GetValue( "Exceptions", typeof( List<DelayedException> ) ) as List<DelayedException>;
        }

        public DelayedExceptionList( List<DelayedException> exceptions, object targetObject, string targetObjectId )
            : base( FormatMessage( exceptions[0].Message, targetObject, targetObjectId ), exceptions[0] )
        {
            this.exceptions = exceptions;
        }

        public static string FormatMessage( string errorMessage, object targetObject, string targetObjectId )
        {
            string str = "";
            if ( targetObject != null )
            {
                str = "'" + targetObject.GetType() + "', ";
            }
            if ( !string.IsNullOrEmpty( targetObjectId ) )
            {
                str = str + "'" + targetObjectId + "'";
            }
            str = str.TrimEnd( new char[] { ',', ' ' } );
            if ( !string.IsNullOrEmpty( str ) )
            {
                return ( errorMessage + ". " + str );
            }
            return errorMessage;
        }

        public override void GetObjectData( SerializationInfo info, StreamingContext context )
        {
            Guard.ArgumentNotNull( info, "info" );
            info.AddValue( "Exceptions", this.Exceptions, typeof( List<DelayedException> ) );
            base.GetObjectData( info, context );
        }

        // Properties
        public List<DelayedException> Exceptions
        {
            get
            {
                return this.exceptions;
            }
        }
    }

    public static class PathHelper
    {
        // Methods
        public static string GetApplicationFolder()
        {
            return AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        }
    }

    public class Tracing : IDisposable
    {
        // Fields
        private List<string> cache;
        public const string DateTimeFormat = "dd.MM.yy HH:mm:ss.fff";
        private static string defaultLevel = "0";
        public const char FieldDelimiter = '\t';
        private static bool hasPermissionToGetAssemblyName = true;
        private static bool? hasUnmanagedCodePermission = null;
        private List<string> lastEntries;
        private int lastEntriesMaxCount;
        private const int lastEntriesMaxCountDefault = 100;
        private TextWriterTraceListener listener;
        private int lockCount;
        private static object lockObject = new object();
        public static string LogName = "eXpressAppFramework";
        private static string outputDirectory;
        private static readonly string sectionDelim = new string( '=', 80 );
        private static readonly string subSectionDelim = new string( '-', 80 );
        public const string SwitchName = "eXpressAppFramework";
        public const string TraceListenerName = "XAFTraceListener";
        private static bool traceLockedSections = false;
        private static Tracing tracer;
        private static Dictionary<Guid, Tracing> tracingDictionary = new Dictionary<Guid, Tracing>();
        private TraceSwitch verbositySwitch;

        // Events
        public static event EventHandler<CustomFormatDateTimeStampEventArgs> CustomFormatDateTimeStamp;

        public static event EventHandler<NeedContextInformationEventArgs> NeedContextInformation;

        // Methods
        private Tracing()
        {
            this.cache = new List<string>();
            this.lastEntriesMaxCount = 100;
            this.lastEntries = new List<string>( 100 );
            if ( AppDomain.CurrentDomain.FriendlyName.ToLower().Contains( "domain-nunit.addin" ) )
            {
                lock ( lockObject )
                {
                    for ( int i = Trace.Listeners.Count - 1; i >= 0; i-- )
                    {
                        if ( Trace.Listeners[i] is DefaultTraceListener )
                        {
                            Trace.Listeners.RemoveAt( i );
                        }
                    }
                }
            }
            this.verbositySwitch = new TraceSwitch( "eXpressAppFramework", "0-Off, 1-Errors, 2-Warnings, 3-Info, 4-Verbose", defaultLevel );
            if ( HasUnmanagedCodePermission )
            {
                try
                {
                    this.AddTraceLogListener();
                    InitializeTraceAutoFlush();
                }
                catch ( SecurityException )
                {
                }
            }
            this.LogHeader();
            this.LogStartupInformation();
        }

        private Tracing( string filename )
        {
            this.cache = new List<string>();
            this.lastEntriesMaxCount = 100;
            this.lastEntries = new List<string>( 100 );
            this.listener = new TextWriterTraceListener( filename, "XAFTraceListener" );
            this.verbositySwitch = new TraceSwitch( "eXpressAppFramework", "0-Off, 1-Errors, 2-Warnings, 3-Info, 4-Verbose", "1" );
        }

        private void AddTraceLogListener()
        {
            if ( ( this.verbositySwitch.Level != TraceLevel.Off ) && !string.IsNullOrEmpty( OutputDirectory ) )
            {
                TextWriterTraceListener listener = new TextWriterTraceListener( Path.Combine( OutputDirectory, LogName + ".log" ), "XAFTraceListener" );
                lock ( Trace.Listeners )
                {
                    Trace.Listeners.Remove( "XAFTraceListener" );
                    Trace.Listeners.Add( listener );
                }
            }
        }

        public static void Close()
        {
            if ( tracer != null )
            {
                tracer.Dispose();
                tracer = null;
            }
        }

        public static void Close( bool deleteLog )
        {
            Close();
            if ( deleteLog )
            {
                File.Delete( Path.Combine( OutputDirectory, LogName + ".log" ) );
            }
        }

        public void Dispose()
        {
            try
            {
                lock ( Trace.Listeners )
                {
                    if ( Trace.Listeners["XAFTraceListener"] != null )
                    {
                        TraceListener listener = Trace.Listeners["XAFTraceListener"];
                        Trace.Listeners.Remove( "XAFTraceListener" );
                        listener.Dispose();
                    }
                }
            }
            catch
            {
            }
        }

        private string EnumerateNetFrameworkVersions()
        {
            string str3;
            try
            {
                using ( RegistryKey key = Registry.LocalMachine.OpenSubKey( @"SOFTWARE\Microsoft\.NETFramework", RegistryKeyPermissionCheck.Default, RegistryRights.QueryValues ) )
                {
                    List<string> list = new List<string>();
                    string path = (string)key.GetValue( "InstallRoot", "" );
                    foreach ( string str2 in Directory.GetDirectories( path ) )
                    {
                        if ( File.Exists( Path.Combine( str2, "fusion.dll" ) ) )
                        {
                            list.Add( Path.GetFileName( str2 ) );
                        }
                    }
                    str3 = string.Join( ", ", list.ToArray() );
                }
            }
            catch ( Exception exception )
            {
                str3 = string.Format( "Unable to enumerate .Net Framework versions. Error: {0}", exception.Message );
            }
            return str3;
        }

        private void FlushCache()
        {
            if ( this.cache.Count > 0 )
            {
                if ( HasUnmanagedCodePermission )
                {
                    if ( this.listener != null )
                    {
                        this.listener.WriteLine( string.Join( "\r\n", this.cache.ToArray() ) );
                        this.listener.Close();
                    }
                    else
                    {
                        Trace.WriteLine( string.Join( "\r\n", this.cache.ToArray() ) );
                    }
                }
                this.cache.Clear();
            }
        }

        public string FormatExceptionReport( Exception exception )
        {
            return FormatExceptionReportDefault( exception );
        }

        private static void FormatExceptionReport( Exception exception, List<string> report, string indent )
        {
            indent = indent + "\t";
            report.Add( indent + "Type:       " + exception.GetType().Name );
            report.Add( indent + "Message:    " + exception.Message );
            report.Add( string.Concat( new object[] { indent, "Data:       ", exception.Data.Count, " entries" } ) );
            if ( exception is DelayedExceptionList )
            {
                int num = 0;
                foreach ( DelayedException exception2 in ( (DelayedExceptionList)exception ).Exceptions )
                {
                    report.Add( indent + "-------------------" );
                    report.Add( string.Concat( new object[] { indent, "Delayed exception ", num, ":" } ) );
                    report.Add( "" );
                    FormatExceptionReport( exception2, report, indent );
                    num++;
                }
            }
            else
            {
                foreach ( object obj2 in exception.Data.Keys )
                {
                    object obj3 = exception.Data[obj2];
                    string str = ( obj3 == null ) ? "null" : obj3.ToString();
                    report.Add( string.Concat( new object[] { indent, "\t\t'", obj2, "'\t\t'", str, "'" } ) );
                    if ( ( obj3 is Exception ) && ( obj3 != exception ) )
                    {
                        FormatExceptionReport( (Exception)obj3, report, indent + "\t" );
                    }
                }
                ReflectionTypeLoadException exception3 = exception as ReflectionTypeLoadException;
                if ( exception3 != null )
                {
                    report.Add( string.Concat( new object[] { indent, "LoaderExceptions:       ", exception3.LoaderExceptions.Length, " entries" } ) );
                    if ( exception3.LoaderExceptions.Length > 0 )
                    {
                        report.Add( indent + "\t\t'0'\t\t'" + exception3.LoaderExceptions[0].Message + "'" );
                    }
                }
                report.Add( indent + "Stack trace:" );
                report.Add( "" );
                report.Add( exception.StackTrace );
                if ( exception.InnerException != null )
                {
                    report.Add( indent + "----------------" );
                    report.Add( indent + "InnerException:" );
                    report.Add( "" );
                    FormatExceptionReport( exception.InnerException, report, indent + "\t" );
                }
                else
                {
                    report.Add( indent + "InnerException is null" );
                    report.Add( string.Empty );
                }
            }
        }

        public static string FormatExceptionReportDefault( Exception exception )
        {
            List<string> report = new List<string> {
            sectionDelim,
            "The error occurred:",
            ""
        };
            FormatExceptionReport( exception, report, "" );
            FormatLoadedAssemblies( report );
            report.Add( sectionDelim );
            report.Add( string.Empty );
            return string.Join( "\r\n", report.ToArray() );
        }

        private static void FormatLoadedAssemblies( List<string> report )
        {
            report.Add( subSectionDelim );
            report.Add( "Loaded assemblies" );
            List<string> collection = new List<string>();
            try
            {
                bool flag = false;
                foreach ( Assembly assembly in AppDomain.CurrentDomain.GetAssemblies() )
                {
                    string location = "";
                    if ( !( assembly is AssemblyBuilder ) && ( assembly.GetType().FullName != "System.Reflection.Emit.InternalAssemblyBuilder" ) )
                    {
                        if ( !flag )
                        {
                            try
                            {
                                location = assembly.Location;
                            }
                            catch ( SecurityException exception )
                            {
                                flag = true;
                                location = exception.Message;
                            }
                        }
                    }
                    else
                    {
                        location = "InMemory Module";
                    }
                    collection.Add( string.Format( "\t{0}, Location={1}", assembly.FullName, location ) );
                }
            }
            catch ( Exception exception2 )
            {
                collection.Add( exception2.Message );
            }
            collection.Sort();
            report.AddRange( collection );
        }

        private static Version GetAssemblyVersion( Assembly assembly )
        {
            if ( hasPermissionToGetAssemblyName )
            {
                try
                {
                    return assembly.GetName().Version;
                }
                catch ( SecurityException )
                {
                    hasPermissionToGetAssemblyName = false;
                }
            }
            string fullName = "";
            fullName = assembly.FullName;
            int index = fullName.IndexOf( ',' );
            return new Version( fullName.Substring( fullName.IndexOf( '=', index ) + 1, ( fullName.IndexOf( ',', fullName.IndexOf( '=', index ) ) - fullName.IndexOf( '=', index ) ) - 1 ) );
        }

        private string GetDateTimeStamp()
        {
            DateTime now = DateTime.Now;
            CustomFormatDateTimeStampEventArgs e = new CustomFormatDateTimeStampEventArgs( now, now.ToString( "dd.MM.yy HH:mm:ss.fff", DateTimeFormatInfo.InvariantInfo ) );
            if ( CustomFormatDateTimeStamp != null )
            {
                CustomFormatDateTimeStamp( this, e );
            }
            return e.Result;
        }

        public string GetLastEntriesAsString()
        {
            lock ( this )
            {
                return string.Join( "\n", this.lastEntries.ToArray() );
            }
        }

        public static void Initialize( string outputDirectory )
        {
            Initialize( outputDirectory, defaultLevel );
        }

        public static void Initialize( Guid key, string outputFile )
        {
            if ( tracingDictionary.ContainsKey( key ) )
            {
                tracingDictionary[key].Dispose();
                tracingDictionary.Remove( key );
            }
            Tracing tracing = new Tracing( outputFile );
            tracingDictionary.Add( key, tracing );
        }

        public static void Initialize( string outputDirectory, string defaultLevel )
        {
            Close();
            Tracing.defaultLevel = defaultLevel;
            Tracing.outputDirectory = outputDirectory;
        }

        private static void InitializeTraceAutoFlush()
        {
            Trace.AutoFlush = true;
        }

        public void LockFlush()
        {
            lock ( this )
            {
                this.lockCount++;
            }
        }

        public void LogError( Exception exception )
        {
            this.LogError( this.FormatExceptionReport( exception ), new object[0] );
        }

        public static void LogError( Guid key, Exception exception )
        {
            if ( tracingDictionary.ContainsKey( key ) )
            {
                tracingDictionary[key].LogError( exception );
            }
        }

        public void LogError( string text, params object[] args )
        {
            this.WriteLineIfFormat( this.verbositySwitch.TraceError, text, args );
            lock ( this )
            {
                this.FlushCache();
            }
        }

        private void LogHeader()
        {
            this.WriteLineIf( this.verbositySwitch.Level != TraceLevel.Off, sectionDelim );
            this.WriteLineIfFormat( this.verbositySwitch.Level != TraceLevel.Off, "Trace Log for {0} is started", new object[] { AppDomain.CurrentDomain.FriendlyName } );
            this.WriteLineIf( this.verbositySwitch.Level != TraceLevel.Off, sectionDelim );
        }

        public void LogLoadedAssemblies()
        {
            List<string> report = new List<string>();
            FormatLoadedAssemblies( report );
            this.LogText( string.Join( "\r\n", report.ToArray() ), new object[0] );
        }

        public void LogLockedSectionEntered()
        {
            if ( traceLockedSections && this.verbositySwitch.TraceVerbose )
            {
                this.LogVerboseText( "Lock section entered", new object[0] );
            }
        }

        public void LogLockedSectionEntering( Type type, string methodName, object lockObject )
        {
            if ( traceLockedSections && this.verbositySwitch.TraceVerbose )
            {
                this.LogVerboseText( string.Concat( new object[] { "Lock section entering :", type, ".", methodName, ", ", lockObject.GetHashCode() } ), new object[0] );
            }
        }

        public void LogSeparator( string comment )
        {
            this.LogText( comment, new object[0] );
            if ( !string.IsNullOrEmpty( comment ) )
            {
                this.LogText( new string( '=', comment.Length + 1 ), new object[0] );
            }
        }

        public void LogSetOfStrings( params string[] args )
        {
            this.WriteLineIf( this.verbositySwitch.TraceInfo, string.Join( ", ", args ) );
        }

        private void LogStartupInformation()
        {
            List<string> list = new List<string> {
            "System Environment",
            "\tOS Version: " + Environment.OSVersion.VersionString,
            "\t.Net Framework Versions: " + this.EnumerateNetFrameworkVersions(),
            string.Empty,
            "\tCLR Version: " + Environment.Version.ToString(),
            "\teXpressApp Version: " + GetAssemblyVersion(base.GetType().Assembly),
            "Application config"
        };
            foreach ( string str in ConfigurationManager.AppSettings.AllKeys )
            {
                list.Add( string.Format( "\t{0}={1}", str, ConfigurationManager.AppSettings[str] ) );
            }
            this.LogText( string.Join( "\r\n", list.ToArray() ), new object[0] );
        }

        public void LogSubSeparator( string comment )
        {
            this.LogText( subSectionDelim, new object[0] );
            this.LogText( comment, new object[0] );
        }

        public void LogText( string text, params object[] args )
        {
            this.WriteLineIfFormat( this.verbositySwitch.TraceInfo, text, args );
        }

        public static void LogText( Guid key, string text, params object[] args )
        {
            tracingDictionary[key].LogText( text, args );
        }

        public void LogValue( string valueName, object objectValue )
        {
            this.LogText( "\t{0}: {1}", new object[] { valueName, this.ValueToString( objectValue ) } );
        }

        public static void LogValue( Guid key, string valueName, object objectValue )
        {
            tracingDictionary[key].LogValue( valueName, objectValue );
        }

        public void LogVerboseSubSeparator( string comment )
        {
            this.LogVerboseText( subSectionDelim, new object[0] );
            this.LogVerboseText( comment, new object[0] );
        }

        public void LogVerboseText( string text, params object[] args )
        {
            this.WriteLineIfFormat( this.verbositySwitch.TraceVerbose, text, args );
        }

        public void LogVerboseValue( string valueName, object objectValue )
        {
            this.LogVerboseText( "\t{0}: {1}", new object[] { valueName, this.ValueToString( objectValue ) } );
        }

        public void LogWarning( string text, params object[] args )
        {
            this.WriteLineIfFormat( this.verbositySwitch.TraceWarning, text, args );
        }

        private void RaiseNeedContextInformation( NeedContextInformationEventArgs args )
        {
            if ( NeedContextInformation != null )
            {
                NeedContextInformation( this, args );
            }
        }

        public void ResumeFlush()
        {
            lock ( this )
            {
                if ( this.lockCount > 0 )
                {
                    this.lockCount--;
                    if ( ( this.lockCount == 0 ) && ( this.cache.Count > 0 ) )
                    {
                        this.FlushCache();
                    }
                }
            }
        }

        private string ValueToString( object value )
        {
            return this.ValueToString( value, new List<object>() );
        }

        private string ValueToString( object value, List<object> list )
        {
            string str = "<not specified>";
            if ( list.IndexOf( value ) != -1 )
            {
                return "<recursive reference>";
            }
            list.Add( value );
            if ( value != null )
            {
                Array array = value as Array;
                if ( array != null )
                {
                    if ( array.Length > 0 )
                    {
                        List<string> list2 = new List<string> {
                        string.Empty
                    };
                        foreach ( object obj2 in array )
                        {
                            list2.Add( "\t\t" + this.ValueToString( obj2, list ) );
                        }
                        str = string.Join( "\r\n", list2.ToArray() );
                    }
                }
                else
                {
                    str = value.ToString();
                }
            }
            list.Remove( value );
            return str;
        }

        private void WriteLineIf( bool condition, string text )
        {
            NeedContextInformationEventArgs args = new NeedContextInformationEventArgs();
            this.RaiseNeedContextInformation( args );
            this.WriteLineIf( condition, args.ContextInformation, text );
        }

        private void WriteLineIf( bool condition, string contextInfo, string text )
        {
            lock ( this )
            {
                string str = !string.IsNullOrEmpty( text ) ? ( ( text.Length >= 100 ) ? text.Substring( 0, 100 ) : text ) : text;
                string str2 = string.Concat( new object[] { this.GetDateTimeStamp(), '\t', contextInfo, string.IsNullOrEmpty( contextInfo ) ? "" : '\t'.ToString() } );
                if ( this.lastEntriesMaxCount > 0 )
                {
                    this.lastEntries.Add( str2 + str );
                    if ( ( this.lastEntriesMaxCount > 0 ) && ( this.lastEntries.Count > this.lastEntriesMaxCount ) )
                    {
                        this.lastEntries.RemoveRange( 0, this.lastEntries.Count - this.lastEntriesMaxCount );
                    }
                }
                if ( condition )
                {
                    if ( this.lockCount == 0 )
                    {
                        if ( this.listener != null )
                        {
                            this.listener.WriteLine( str2 + text );
                            this.listener.Close();
                        }
                        else if ( HasUnmanagedCodePermission )
                        {
                            Trace.WriteLine( str2 + text );
                        }
                    }
                    else
                    {
                        this.cache.Add( str2 + ( !string.IsNullOrEmpty( text ) ? ( ( text.Length >= 0x1388 ) ? text.Substring( 0, 0x1388 ) : text ) : text ) );
                    }
                }
            }
        }

        private void WriteLineIfFormat( bool condition, string textFormat, params object[] args )
        {
            if ( args.Length == 0 )
            {
                this.WriteLineIf( condition, textFormat );
            }
            else
            {
                this.WriteLineIf( condition, string.Format( textFormat, args ) );
            }
        }

        // Properties
        internal static string DefaultLevel
        {
            get
            {
                return defaultLevel;
            }
        }

        private static bool HasUnmanagedCodePermission
        {
            get
            {
                lock ( lockObject )
                {
                    if ( !hasUnmanagedCodePermission.HasValue )
                    {
                        hasUnmanagedCodePermission = false;
                        try
                        {
                            if ( SecurityManager.IsGranted( new SecurityPermission( SecurityPermissionFlag.UnmanagedCode ) ) )
                            {
                                new SecurityPermission( SecurityPermissionFlag.UnmanagedCode ).Demand();
                                hasUnmanagedCodePermission = true;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                return hasUnmanagedCodePermission.Value;
            }
        }

        public ReadOnlyCollection<string> LastEntries
        {
            get
            {
                lock ( this )
                {
                    return this.lastEntries.AsReadOnly();
                }
            }
        }

        public int LastEntriesMaxCount
        {
            get
            {
                return this.lastEntriesMaxCount;
            }
            set
            {
                lock ( this )
                {
                    this.lastEntriesMaxCount = value;
                    if ( this.lastEntries.Capacity < this.lastEntriesMaxCount )
                    {
                        this.lastEntries.Capacity = this.lastEntriesMaxCount;
                    }
                }
            }
        }

        private static string OutputDirectory
        {
            get
            {
                lock ( lockObject )
                {
                    if ( outputDirectory == null )
                    {
                        try
                        {
                            outputDirectory = PathHelper.GetApplicationFolder();
                        }
                        catch ( SecurityException )
                        {
                            outputDirectory = "";
                        }
                    }
                }
                return outputDirectory;
            }
        }

        public static bool TraceLockedSections
        {
            get
            {
                return traceLockedSections;
            }
            set
            {
                traceLockedSections = value;
            }
        }

        public static Tracing Tracer
        {
            get
            {
                if ( tracer == null )
                {
                    tracer = new Tracing();
                }
                return tracer;
            }
        }

        public TraceSwitch VerbositySwitch
        {
            get
            {
                return this.verbositySwitch;
            }
        }
    }

}
