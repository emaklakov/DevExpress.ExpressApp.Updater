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
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.Win.Utils;

namespace DevExpress.ExpressApp.Updater 
{

	public class MainClass 
    {
		private static ProgressWindow form;

        private static List<Process> GetCurrentUserProcessByName( string name )
        {
            List<Process> currentUserProcess = new List<Process>();
            foreach( Process process in Process.GetProcessesByName( Path.GetFileNameWithoutExtension( name ) ) )
            {
                if( process.SessionId == Process.GetCurrentProcess().SessionId )
                {
                    currentUserProcess.Add( process );
                }
            }
            return currentUserProcess;
        }

        private static Boolean CloseAllApplications( string name, int applicationId )
        {
            if( applicationId != -1 )
            {
                Tracing.Tracer.LogText( "try to kill process '{0}'", applicationId );
                Process mainProcess = null;
                try
                {
                    mainProcess = Process.GetProcessById( applicationId );
                }
                catch( ArgumentException e )
                {
                    Tracing.Tracer.LogText( e.Message );
                }

                if( mainProcess != null )
                {
                    mainProcess.Kill();
                    mainProcess.WaitForExit();
                }
            }

            Tracing.Tracer.LogText( "Close all applications with name '{0}'", name );

            foreach( Process process in GetCurrentUserProcessByName( name ) )
            {
                Tracing.Tracer.LogText( "Try to close process '{0}'", process.Id );
                try
                {
                    while( process.CloseMainWindow() )
                    {
                        Thread.Sleep( 500 );
                        process.Refresh();
                    }
                }
                catch( Exception exception )
                {
                    Tracing.Tracer.LogError( exception );
                }

                try
                {
                    process.WaitForExit( 3500 );
                }
                catch( Exception exception )
                {
                    Tracing.Tracer.LogError( exception );
                }
            }
            return GetCurrentUserProcessByName( name ).Count == 0;
        }

		[STAThread]
		public static void Main(string[] args) 
        {
			//Tracing.Tracer.LogText("args");
			//Tracing.Tracer.LogSetOfStrings(args);

            if( args.Length == 0 )
            {
                MessageBox.Show("Параметры не могут быть пустыми.", "Внимание", MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
            }

			if(args.Length >= 1) 
            {
				String applicationName = "";
				int applicationId = -1;

				if(args.Length > 1) 
                {
					applicationName = args[1];
				}

				if(args.Length > 2) 
                {
					applicationId = Int32.Parse(args[2]);
				}

				if(!String.IsNullOrEmpty(applicationName) && !CloseAllApplications(applicationName, applicationId)) 
                {
					MessageBox.Show(
						"The update process of the starting application cannot be finished, " +
						"because other instances of this application cannot be closed. " +
						"Close these applications manually and start the application again.",
						"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else 
                {
                    // Произодим создание формы
                    form = new DevExpress.ExpressApp.Win.Utils.ProgressWindow();
                    form.Param = args;

                    try
                    {
                        Application.EnableVisualStyles();
                        Application.Run( form );
                    }
                    catch(Exception error)
                    {
                        MessageBox.Show( error.Message, "Application Updater", MessageBoxButtons.OK, MessageBoxIcon.Error );
                    }
                }
			}
		}
	}
}
