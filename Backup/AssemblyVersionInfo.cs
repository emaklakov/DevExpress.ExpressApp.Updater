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

using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Resources;
[assembly: AssemblyVersion(AssemblyInfo.Version)]
[assembly: AssemblyFileVersion(DevExpress.ExpressApp.Utils.Consts.FileVersion)]
[assembly: SatelliteContractVersion(AssemblyInfo.VersionShort + ".0.0")]
[assembly: AssemblyCompany(AssemblyInfo.AssemblyCompany)]
[assembly: AssemblyCopyright(AssemblyInfo.AssemblyCopyright)]
[assembly: AssemblyTrademark("ALL RIGHTS RESERVED")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyName("")]
[assembly: ComVisible(false)]
[assembly: AllowPartiallyTrustedCallers()]
[assembly: NeutralResourcesLanguage("en-US")]
namespace DevExpress.ExpressApp.Utils {
	internal class Consts {
		public const string FileVersion = "12.2.5.0";
	}
}
