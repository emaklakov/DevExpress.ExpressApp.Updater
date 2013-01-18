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
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using DevExpress.ExpressApp.Updater;
using DevExpress.Persistent.Base;
using System.Diagnostics;

namespace DevExpress.ExpressApp.Win.Utils 
{
	public class ProgressWindow : Form 
    {
        private string folderUpdate = Application.StartupPath + "\\UpdateTemp\\";
        string filePath = "";
		private ProgressBar progressBar;
	    private string[] param = {""};

		public Icon GetExecutingApplicationIcon()
		{
            if( File.Exists( Directory.GetCurrentDirectory() + "\\DevExpress.ExpressApp.Updater.exe" ) )
                return Icon.ExtractAssociatedIcon( Directory.GetCurrentDirectory() + "\\DevExpress.ExpressApp.Updater.exe" );			

			return null;
		}

        #region XAF Code

        private int GetFilesCountInSubDirectories( string directory )
        {
            int filesCount = 0;
            string[] subDirectories = Directory.GetDirectories( directory );
            foreach( string subDirectory in subDirectories )
            {
                filesCount += GetFilesCount( subDirectory );
            }
            return filesCount;
        }

        private int GetFilesCount( string directory )
        {
            int filesCount = Directory.GetFiles( directory, "*.*" ).Length;
            filesCount += GetFilesCountInSubDirectories( directory );
            return filesCount;
        }

        private ICollection<string> GetSelfFiles( string sourceDirectory )
        {
            List<string> result = new List<string>();
            result.Add( Path.Combine( sourceDirectory, AppDomain.CurrentDomain.FriendlyName ) );
            result.Add( Path.Combine( sourceDirectory, Tracing.LogName + ".log" ) );
            return result;
        }

        private void CopyNewVersion( string sourceDirectory, string destinationDirectory )
        {
            if( Directory.Exists( destinationDirectory ) )
            {
                Tracing.Tracer.LogText( "CopyNewVersion from '{0}' to '{1}'", sourceDirectory, destinationDirectory );
                string[] sourceFiles = Directory.GetFiles( sourceDirectory, "*.*" );
                ICollection<string> selfFiles = GetSelfFiles( sourceDirectory );
                foreach( string sourceFileName in sourceFiles )
                {
                    if( !selfFiles.Contains( sourceFileName ) )
                    {
                        string destinationFileName = Path.Combine( destinationDirectory, Path.GetFileName( sourceFileName ) );
                        if( File.Exists( destinationFileName ) )
                        {
                            File.SetAttributes( destinationFileName, FileAttributes.Normal );
                        }
                        File.Copy( sourceFileName, destinationFileName, true );
                        Tracing.Tracer.LogText( "The \"{0}\" file was copied to \"{1}\".", sourceFileName, destinationFileName );
                        this.SetProgressPosition();
                    }
                }
                UpdateSubDirectories( sourceDirectory, destinationDirectory );
            }
        }

        private void UpdateSubDirectories( string sourceDirectory, string destinationDirectory )
        {
            Tracing.Tracer.LogText( "Update sub directories from '{0}' to '{1}'", sourceDirectory, destinationDirectory );
            string[] sourceSubDirectories = Directory.GetDirectories( sourceDirectory );
            foreach( string sourceSubDirectory in sourceSubDirectories )
            {
                string destinationSubDirectory = destinationDirectory + sourceSubDirectory.Remove( 0, sourceDirectory.Length );
                if( !Directory.Exists( destinationSubDirectory ) )
                {
                    Directory.CreateDirectory( destinationSubDirectory );
                    Tracing.Tracer.LogText( "Directory '{0}' was created.", destinationSubDirectory );
                }
                CopyNewVersion( sourceSubDirectory, destinationSubDirectory );
            }
        }

        #endregion XAF Code

        // Производи распоковку Zip архива
        private void ExtractZip( string filePath, string folder )
        {

            try
            {
                //Open an existing zip file for reading
                using( ZipStorer zip = ZipStorer.Open( filePath, FileAccess.Read ) )
                {
                    // Read the central directory collection
                    List<ZipStorer.ZipFileEntry> dir = zip.ReadCentralDir();

                    this.SetProgressPosition( dir.Count, 0 );

                    // Look for the desired file
                    foreach( ZipStorer.ZipFileEntry entry in dir )
                    {
                        zip.ExtractFile( entry, folder + entry.FilenameInZip );
                        this.SetProgressPosition();
                    }
                }

                File.Delete( filePath );

                this.SetProgressPosition( GetFilesCount( folder ), 0 ); // Получаем поличество файлов в папке обновления
                CopyNewVersion( folder, AppDomain.CurrentDomain.BaseDirectory );

                EndUpdate();
            }
            catch(Exception error)
            {
                MessageBox.Show( error.Message, "Application Updater", MessageBoxButtons.OK, MessageBoxIcon.Error );

                if ( Directory.Exists( folderUpdate ) )
                {
                    Directory.Delete( folderUpdate, true );
                }
                
                this.Close();
            }
        }

        // Производим скачивание обновления для программы
        private void DownloadNewVersion( string Uri )
        {
            // Производим создание папки для обновления
            Directory.CreateDirectory( folderUpdate );

            filePath = folderUpdate + "Update.zip";

            // Делаем загрузку с помощью System.Net.WebClient.
            var webClient = new WebClient();

            webClient.DownloadProgressChanged += ( s, ev ) =>
            {
                this.SetProgressPosition( 100, ev.ProgressPercentage );
            };

            // подписываемся на событие окончания загрузки, чтобы знать когда загрузка закончится
            webClient.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler( webClient_DownloadFileCompleted );

            // запускаем загрузку асинхронно.
            webClient.DownloadFileAsync( new Uri( Uri ), filePath );
        }

        void webClient_DownloadFileCompleted( object sender, System.ComponentModel.AsyncCompletedEventArgs e )
        {
            if ( e.Error != null )
            {
                MessageBox.Show( e.Error.Message, "Application Updater", MessageBoxButtons.OK, MessageBoxIcon.Error );

                if ( Directory.Exists( folderUpdate ) )
                {
                    Directory.Delete( folderUpdate, true );
                }

                this.Close();
                return;
            }

            if (!e.Cancelled)
            {
                ExtractZip( filePath, folderUpdate );
            }
        }

		public ProgressWindow() : base() 
        {
			int Padding = 10;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            MinimizeBox = false;
            MaximizeBox = false;
            HelpButton = false;
            ShowInTaskbar = true;
			Icon = GetExecutingApplicationIcon();
			Text = Application.ProductName;
			Panel place = new Panel();
			place.Location = new Point(0, 0);
			place.Dock = DockStyle.Fill;
			place.BorderStyle = BorderStyle.FixedSingle;
			Controls.Add(place);
			PictureBox picture = new PictureBox();
			picture.SizeMode = PictureBoxSizeMode.AutoSize;
			picture.Image = Icon.ToBitmap();
			picture.Location = new System.Drawing.Point(Padding, Padding);
			place.Controls.Add(picture);
			Label waitLabel = new Label();
			waitLabel.AutoSize = true;
			waitLabel.Location = new System.Drawing.Point(Padding * 2 + picture.Width, 0);
            waitLabel.Text = "  Обновление приложения до последней версии...  ";
			place.Controls.Add(waitLabel);
			Size = new Size(picture.Width + waitLabel.Width + Padding * 3, picture.Height + Padding * 2 + 10);
			waitLabel.Top = (picture.Height - waitLabel.Height)/2 + Padding - 5;
			progressBar = new System.Windows.Forms.ProgressBar();
			progressBar.Minimum = 0;
			progressBar.Value = 0;
			progressBar.Step = 1;
			progressBar.Location = new System.Drawing.Point(Padding * 2 + picture.Width + 8, 35);
			progressBar.Size = new Size(waitLabel.Width - 15, 10);
			place.Controls.Add(progressBar);
            this.Shown += OnShown;
        }

        /// <summary>
        /// Запускаем обновление после появления формы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
	    private void OnShown(object sender, EventArgs eventArgs)
	    {
	        StartUpdate();
	    }

	    private void StartUpdate()
        {
            try
            {
                Uri uri = new Uri( Param[0].ToString() ); // Uri для проверки

                // Проверяем откуда будем скачивать: Web или Local
                if ( uri.IsFile )
                {
                    // Производим скачивание с локального сервера
                    this.Maximum = GetFilesCount( Param[0] ); // Получаем поличество файлов в папке обновления
                    CopyNewVersion( Param[0], AppDomain.CurrentDomain.BaseDirectory );

                    EndUpdate();
                }
                else 
                {
                    DownloadNewVersion( Param[0].ToString() ); // Производим скачивание файла
                }
            }
            catch( Exception error )
            {
                //Tracing.Tracer.LogError( error );
                Tracing.Tracer.LogText( "Error: " + error.Message );
                MessageBox.Show( error.Message, "Application Updater", MessageBoxButtons.OK, MessageBoxIcon.Error );

                if ( Directory.Exists( folderUpdate ) )
                {
                    Directory.Delete( folderUpdate, true );
                }

                this.Close();
            }
        }

        /// <summary>
        /// Завершаем обновление. Если нужно, то запускаем обновленную программу
        /// </summary>
        private void EndUpdate()
        {
            if( Param.Length > 1 )
            {
                Process.Start( Path.Combine( AppDomain.CurrentDomain.BaseDirectory, Param[1] ), "ApplicationUpdateComplete" );
            }

            if ( Directory.Exists( folderUpdate ) )
            {
                Directory.Delete( folderUpdate, true );
            }
            
            this.Close();
        }

        public string[] Param
        {
            get
            {
                return param;
            }
            set
            {
                param = value;
            }
        }

		public int Maximum 
        {
			get { return progressBar.Maximum; }
			set { progressBar.Maximum = value; }
		}

		public void SetProgressPosition() 
        {
			progressBar.Value++;
			Application.DoEvents();
		}

		public void SetProgressPosition(int maximum, int currentPosiotion) 
        {
			progressBar.Maximum = maximum;
			progressBar.Value = currentPosiotion;
		}

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ProgressWindow
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProgressWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ResumeLayout(false);

        }
	}
}
