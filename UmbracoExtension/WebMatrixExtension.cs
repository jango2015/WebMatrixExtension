using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.Linq;
using System.Web.Configuration;
using System.Windows.Controls;
using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.Extensibility.Editor;

namespace UmbracoExtension
{
    /// <summary>
    /// An Umbraco WebMatrix extension.
    /// </summary>
    [Export(typeof(Extension))]
    public class UmbracoExtension :  Extension
    {
        #region IExtension Members

        private string _dbType
        {
            get
            {
                // make sure we have an Umbraco site
                if (WebMatrixHost.WebSite.ApplicationIdentifier.Contains("Umbraco"))
                {
                    //map the site's config since its not in the current WebMatrix context
                    WebConfigurationFileMap fileMap = new WebConfigurationFileMap();
                    fileMap.VirtualDirectories.Add("/", new VirtualDirectoryMapping(_webMatrixHost.WebSite.Path, true));
                    Configuration webConfig = WebConfigurationManager.OpenMappedWebConfiguration(fileMap, "/");

                    AppSettingsSection appSetting = webConfig.AppSettings;
                    string dbType = appSetting.Settings["umbracoDbDSN"].Value.ToString();

                    if (dbType.Contains("SQLCE4Umbraco"))
                        return "SQLCE";
                    else
                        return "SQL";
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        private string _dbConnection
        {
            get
            {
                // make sure we have an Umbraco site
                if (null != WebMatrixHost.WebSite.ApplicationIdentifier && WebMatrixHost.WebSite.ApplicationIdentifier.Contains("Umbraco"))
                {
                    //map the site's config since its not in the current WebMatrix context
                    WebConfigurationFileMap fileMap = new WebConfigurationFileMap();
                    fileMap.VirtualDirectories.Add("/", new VirtualDirectoryMapping(_webMatrixHost.WebSite.Path, true));
                    Configuration webConfig = WebConfigurationManager.OpenMappedWebConfiguration(fileMap, "/");

                    AppSettingsSection appSetting = webConfig.AppSettings;

                    var connectionStringBuilder = new DbConnectionStringBuilder();

                        try
                        {
                            connectionStringBuilder.ConnectionString = appSetting.Settings["umbracoDbDSN"].Value.ToString();
                        }
                        catch (Exception ex)
                        {
                            throw new ArgumentException("Bad connection string.", "connectionString", ex);
                        }

                        connectionStringBuilder.Remove("datalayer");

                        // if full SQL then this, else the other one for SqlCe
                        if (_dbType == "SQL")
                        {
                            return connectionStringBuilder.ConnectionString;
                        }
                        else
                        {
                            // sometimes WebMatrix retuns its runtime path for |DataDirectory| and reports "file not found"
                            // the below is the workaround
                            return "data source=" + webConfig.FilePath.Remove(webConfig.FilePath.IndexOf("web.config")) + "App_Data\\Umbraco.sdf";
                        }
                    }
                    else
                    {
                        return string.Empty;
                    }

            }
        }
        
        private IWebMatrixHost _webMatrixHost;
        
        private RibbonContextualTab _contextualTab;

        private RibbonGroup _ribbonGroup;

        private SqlCeConnection SqlCeConn
        {
            get
            {
                // make sure we have an Umbraco site
                if (WebMatrixHost.WebSite.ApplicationIdentifier.Contains("Umbraco"))
                {
                    try
                    {
                        SqlCeConnection conn = new SqlCeConnection(_dbConnection);
                        conn.Open();
                        return conn;
                    }
                    catch (Exception ex)
                    {
                        _webMatrixHost.ShowNotification(ex.Message + " : " + ex.StackTrace + " : " + ex.InnerException);
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }
            
        #endregion

        protected void SiteLoadedContextualTabs()
        {
                try
                {
                    string dbPath = _dbConnection;

                     IEnumerable<RibbonButton> macroButtons = GetMacroList(dbPath);
                     IEnumerable<RibbonButton> umbracoProperties = GetPropertyList(dbPath);

                    if (null != macroButtons || null != umbracoProperties)
                    {
                    _ribbonGroup = new RibbonGroup(
                   "Umbraco",
                   new List<RibbonItem>
                    {
                        new RibbonMenuButton("Insert Property", umbracoProperties, Properties.Resources.macro16x16, Properties.Resources.macro32x32),
                        new RibbonMenuButton("Insert Snippet", GetSnippetItems(), Properties.Resources.macro16x16, Properties.Resources.macro32x32),
                        new RibbonMenuButton("Insert Macro", macroButtons, Properties.Resources.macro16x16, Properties.Resources.macro32x32)
                    });

                    _contextualTab = new RibbonContextualTab("Umbraco Tools", new RibbonItem[] { _ribbonGroup });

                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to get properties for current Umbraco site.", ex);
                }
            }

        protected void PreLoadedContextualTabs()
        {
                try
                {
                        _ribbonGroup = new RibbonGroup(
                       "Umbraco",
                       new List<RibbonItem>
                    {
                        new RibbonMenuButton("Insert Snippet", GetSnippetItems(), Properties.Resources.macro16x16, Properties.Resources.macro32x32)
                    });

                        _contextualTab = new RibbonContextualTab("Umbraco Tools", new RibbonItem[] { _ribbonGroup });

                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Unable to get properties for current Umbraco site.", ex);
                }
            }

        /// <summary>
        /// Initialize the Extension
        /// </summary>
        public UmbracoExtension() : base("Umbraco Extension") 
        {
            // can't set up the ribbon items here since we don't know the site specifics yet
            // construction done in .WorksoaceChanged event once site is loaded
        }

        #region Events
        /// <summary>
        /// Called when the WebMatrixHost property changes.
        /// </summary>
        protected override void Initialize(IWebMatrixHost host, ExtensionInitData initData)
        {
            WebMatrixHost = host;
        }

        [Import(typeof(IWebMatrixHost))]
        private IWebMatrixHost WebMatrixHost
        {
            get { return _webMatrixHost; }
            set
            {
                if (null != _webMatrixHost)
                {
                    _webMatrixHost.WorkspaceChanged -= new EventHandler<WorkspaceChangedEventArgs>(WebMatrixHost_WorkspaceChanged);
                    _webMatrixHost.WebSiteChanged -= new EventHandler<EventArgs>(WebMatrixHost_WebSiteChanged);
                }
                _webMatrixHost = value;
                if (null != _webMatrixHost)
                {
                    _webMatrixHost.WorkspaceChanged += new EventHandler<WorkspaceChangedEventArgs>(WebMatrixHost_WorkspaceChanged);
                    _webMatrixHost.WebSiteChanged += new EventHandler<EventArgs>(WebMatrixHost_WebSiteChanged);
                }
            }
        }

        private void WebMatrixHost_WebSiteChanged(object sender, EventArgs e)
        {
            // only for umbraco sites
            if(null != WebMatrixHost.WebSite.ApplicationIdentifier && WebMatrixHost.WebSite.ApplicationIdentifier.Contains("Umbraco"))
            {
                // reload Ribbon Contextual Tab when a different site is loaded
                if (ContextualTabItems.Contains(_contextualTab))
                {
                    ContextualTabItems.Remove(_contextualTab);
                }
             
                SiteLoadedContextualTabs();
                ContextualTabItems.Add(_contextualTab);

                // make sure to show if we are in EditorWorkspace and hide otherwise
                if (WebMatrixHost.Workspace is IEditorWorkspace)
                    _contextualTab.IsVisible = true;
                else
                    _contextualTab.IsVisible = false;

            }

        }

        private void WebMatrixHost_WorkspaceChanged(object sender, WorkspaceChangedEventArgs e)
        {
            // only for umbraco sites
            if (null != WebMatrixHost.WebSite.ApplicationIdentifier && WebMatrixHost.WebSite.ApplicationIdentifier.Contains("Umbraco"))
            {

                if (e.NewWorkspace is IEditorWorkspace)
                {
                    if (!ContextualTabItems.Contains(_contextualTab))
                    {
                        ContextualTabItems.Add(_contextualTab);
                    }

                    // _contextualTab could be null if state has been lost or on first install
                    if (null != _contextualTab)
                    {
                        _contextualTab.IsVisible = true;
                    }
                    else
                    {
                        // try to load the data for the current site
                        SiteLoadedContextualTabs();
                        ContextualTabItems.Add(_contextualTab);
                        _contextualTab.IsVisible = true;
                    }
                }
                else if (e.OldWorkspace is IEditorWorkspace)
                {
                    ContextualTabItems.Remove(_contextualTab);
                }

            }
        } 

        #endregion

        #region Ribbon Button Getters

        protected IEnumerable<RibbonButton> GetSnippetItems()
        {
            IEnumerable<RibbonButton> RibbonButtons = new RibbonButton[]
            {new RibbonButton(
                "Insert HTML5 Video",
                new DelegateCommand(HandleHTML5Video),
                "HTML5Video",
                Properties.Resources.macro16x16,
                Properties.Resources.macro32x32),
            new RibbonButton(
                "Breadcrumb",
                new DelegateCommand(HandleBreadcrumb),
                "Breadcrumb",
                Properties.Resources.macro16x16,
                Properties.Resources.macro32x32),
            new RibbonButton(
                "List Sub Pages",
                new DelegateCommand(HandleListSubPages),
                "ListSubPages",
                Properties.Resources.macro16x16,
                Properties.Resources.macro32x32),
            new RibbonButton(
                "Macro with Parameters",
                new DelegateCommand(HandleMacroParameters),
                "MacroParameters",
                Properties.Resources.macro16x16,
                Properties.Resources.macro32x32),
            new RibbonButton(
                "Insert Media",
                new DelegateCommand(HandleInsertMedia),
                "InsertMedia",
                Properties.Resources.macro16x16,
                Properties.Resources.macro32x32),
            new RibbonButton(
                "Insert Navigation",
                new DelegateCommand(HandleNavigation),
                "InsertNavigation",
                Properties.Resources.macro16x16,
                Properties.Resources.macro32x32),
            new RibbonButton(
                "Paging Prototype",
                new DelegateCommand(HandlePaging),
                "Paging",
                Properties.Resources.macro16x16,
                Properties.Resources.macro32x32),
            new RibbonButton(
                "Select Children by Type",
                new DelegateCommand(HandleSelectChildrenByType),
                "SelectChildrenByType",
                Properties.Resources.macro16x16,
                Properties.Resources.macro32x32),
            new RibbonButton(
                "Use Related Links",
                new DelegateCommand(HandleRelatedLinks),
                "RelatedLinks",
                Properties.Resources.macro16x16,
                Properties.Resources.macro32x32),
            new RibbonButton(
                "Site Map",
                new DelegateCommand(HandleSiteMap),
                "SiteMap",
                Properties.Resources.macro16x16,
                Properties.Resources.macro32x32)
            };

            return RibbonButtons;
        }

        protected IEnumerable<Macro> MacroListCE(string dbPath)
        {
            // stop the app explicitly before connecting to SqlCe
            Guid GroupId = new Guid("79B5F484-3138-40B7-BB60-F8600C63664F");
            var command = WebMatrixHost.HostCommands.GetCommand(GroupId, 3); // stop site command
            
            var container = GetEditor(); //new Container();
            
            if (container != null)
            {
                if (command.CanExecute(null))
                    command.Execute(container);
            }

            try
            {
                SqlCeCommand cmd = new SqlCeCommand("select macroAlias, macroName from cmsMacro order by macroName", SqlCeConn);
                SqlCeDataReader r = cmd.ExecuteReader();

                var macroList = new List<Macro>();
                while (r.Read())
                {
                    SqlCeCommand paramCmd = new SqlCeCommand("select p.macroPropertyName, p.macroPropertyAlias, p.macroPropertyHidden,t.macroPropertyTypeRenderType, t.macroPropertyTypeBaseType" +
                    " from cmsMacroProperty p inner join cmsMacroPropertyType t on p.macroPropertyType = t.id" +
                    " where p.macro in (select m.id from cmsMacro m where m.macroAlias ='" + r["macroAlias"].ToString() + "')", SqlCeConn);
                    SqlCeDataReader rParam = paramCmd.ExecuteReader();

                    var paramList = new List<Field>();
                    Macro item;

                    while (rParam.Read())
                    {
                        var paramItem = new Field
                        {
                            Name = rParam["macroPropertyName"].ToString(),
                            Alias = rParam["macroPropertyAlias"].ToString(),
                            DataType = rParam["macroPropertyTypeBaseType"].ToString(),
                            Length = 200,
                            Required = false
                        };

                        paramList.Add(paramItem);

                    }

                    item = new Macro
                    {
                        Name = r["macroName"].ToString(),
                        Alias = r["macroAlias"].ToString(),
                        MacroParams = paramList
                    };

                    macroList.Add(item);

                }

                return macroList;

            }
            catch (Exception ex)
            {
                _webMatrixHost.ShowNotification(ex.Message + " : " + ex.StackTrace + " : " + ex.InnerException);
                return null;
            }
            finally
            {
                if (SqlCeConn != null && SqlCeConn.State == ConnectionState.Open)
                    SqlCeConn.Close();
            }
        }

        protected IEnumerable<Macro> MacroListSQL(string dbPath)
        {
            SqlConnection conn = new SqlConnection();

            try
            {
                conn.ConnectionString = _dbConnection;
                conn.Open();
                SqlCommand cmd = new SqlCommand("select macroAlias, macroName from cmsMacro order by macroName", conn);
                SqlDataReader r = cmd.ExecuteReader();

                var macroList = new List<Macro>();
                while (r.Read())
                {
                    // set specific connection for this command - ouch!
                    SqlConnection rConn = new SqlConnection(_dbConnection);
                    rConn.Open();
                    SqlCommand paramCmd = new SqlCommand(
                    "select p.macroPropertyName, p.macroPropertyAlias, p.macroPropertyHidden,t.macroPropertyTypeRenderType, t.macroPropertyTypeBaseType" +
                    " from cmsMacroProperty p inner join cmsMacroPropertyType t on p.macroPropertyType = t.id" +
                    " where p.macro in (select m.id from cmsMacro m where m.macroAlias ='" + r["macroAlias"].ToString() + "')"
                    , rConn);
                    using (SqlDataReader rParam = paramCmd.ExecuteReader())
                    {
                        var paramList = new List<Field>();

                        while (rParam.Read())
                        {
                            var paramItem = new Field
                            {
                                Name = rParam["macroPropertyName"].ToString(),
                                Alias = rParam["macroPropertyAlias"].ToString(),
                                DataType = rParam["macroPropertyTypeBaseType"].ToString(),
                                Length = 200,
                                Required = false
                            };
                            paramList.Add(paramItem);
                        }

                        var item = new Macro
                        {
                            Name = r["macroName"].ToString(),
                            Alias = r["macroAlias"].ToString(),
                            MacroParams = paramList
                        };

                        macroList.Add(item);
                    }
                    rConn.Close();
                }

                return macroList;
            }
            catch (Exception ex)
            {
                _webMatrixHost.ShowNotification(ex.Message + " : " + ex.StackTrace + " : " + ex.InnerException);
                return null;
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                    conn.Close();
            }
        }

        public IEnumerable<RibbonButton> GetMacroList(string dbPath)
        {
            IEnumerable<Macro> umbracoMacroList;

            switch (_dbType)
            {
                case "SQLCE":
                    umbracoMacroList = MacroListCE(dbPath);
                    break;

                case "SQL":
                    umbracoMacroList = MacroListSQL(dbPath);
                    break;

                default:
                    umbracoMacroList = MacroListSQL(dbPath);
                    break;
            }

            string MacroName = string.Empty;

            if (umbracoMacroList != null)
            {
                List<RibbonButton> RibbonButtons = new List<RibbonButton>();

                foreach (Macro m in umbracoMacroList)
                {
                    string alias = m.Alias;
                    List<Field> macroParams = m.MacroParams;

                    RibbonButtons.Add(new RibbonButton(
                            m.Name,
                            new DelegateCommand(param => true, param => HandleMacroOptions(alias, macroParams)),
                            alias,
                            Properties.Resources.macro16x16,
                            Properties.Resources.macro32x32));
                }

                return RibbonButtons;
            }
            return null;
        }

        private IEnumerable<umbProperty> PropertyListSQL(string dbPath)
        {
            SqlConnection conn = new SqlConnection();

            try
            {
                conn.ConnectionString = _dbConnection;
                conn.Open();
                SqlCommand cmd = new SqlCommand("select distinct alias, name from cmsPropertyType order by name", conn);
                SqlDataReader r = cmd.ExecuteReader();

                umbProperty item;

                var propertyList = new List<umbProperty>();
                while (r.Read())
                {
                    item = new umbProperty
                    {
                        Name = r["name"].ToString(),
                        Alias = r["alias"].ToString()
                    };

                    propertyList.Add(item);

                }

                // add umbraco prevalues
                string[] preValuesSource = { "@createDate", "@creatorName", "@level", "@nodeType", "@nodeTypeAlias", "@pageID", "@pageName", "@parentID", "@path", "@template", "@updateDate", "@writerID", "@writerName" };
                foreach (string s in preValuesSource)
                {
                    propertyList.Add(new umbProperty { Name = s, Alias = s.Replace("@", "") });
                }

                return propertyList;

            }
            catch (Exception ex)
            {
                _webMatrixHost.ShowNotification(ex.Message + " : " + ex.StackTrace + " : " + ex.InnerException);
                return null;
            }
        }

        private IEnumerable<umbProperty> PropertyListCE(string dbPath)
        {
            // stop the app explicitly before connecting to SqlCe
            Guid GroupId = new Guid("79B5F484-3138-40B7-BB60-F8600C63664F");
            var command = WebMatrixHost.HostCommands.GetCommand(GroupId, 3); // stop site command

            var container = GetEditor();

            if (container != null)
            {
                if (command.CanExecute(null))
                    command.Execute(container);
            }

            try
            {
                SqlCeCommand cmd = new SqlCeCommand("select distinct alias, name from cmsPropertyType order by name", SqlCeConn);
                SqlCeDataReader r = cmd.ExecuteReader();

                umbProperty item;

                var propertyList = new List<umbProperty>();
                while (r.Read())
                {
                    item = new umbProperty
                    {
                        Name = r["name"].ToString(),
                        Alias = r["alias"].ToString()
                    };

                    propertyList.Add(item);

                }

                // add umbraco prevalues
                string[] preValuesSource = { "@createDate", "@creatorName", "@level", "@nodeType", "@nodeTypeAlias", "@pageID", "@pageName", "@parentID", "@path", "@template", "@updateDate", "@writerID", "@writerName" };
                foreach (string s in preValuesSource)
                {
                    propertyList.Add(new umbProperty { Name = s, Alias = s.Replace("@", "") });
                }

                return propertyList;

            }
            catch (Exception ex)
            {
                _webMatrixHost.ShowNotification(ex.Message + " : " + ex.StackTrace + " : " + ex.InnerException);
                return null;
            }
            finally
            {
                if (SqlCeConn != null && SqlCeConn.State == ConnectionState.Open)
                    SqlCeConn.Close();
            }
        }

        public IEnumerable<RibbonButton> GetPropertyList(string dbPath)
        {
            IEnumerable<umbProperty> umbracoProperties;

            switch (_dbType)
            {
                case "SQLCE":
                    umbracoProperties = PropertyListCE(dbPath);
                    break;

                case "SQL":
                    umbracoProperties = PropertyListSQL(dbPath);
                    break;

                default:
                    umbracoProperties = PropertyListSQL(dbPath);
                    break;
            }

            if (umbracoProperties != null)
            {
                List<RibbonButton> RibbonButtons = new List<RibbonButton>();

                foreach (umbProperty p in umbracoProperties)
                {
                    string alias = p.Alias;

                    RibbonButtons.Add(new RibbonButton(
                            p.Name,
                            new DelegateCommand(param => true, param => HandlePropertyOptions(alias)),
                            alias,
                            Properties.Resources.macro16x16,
                            Properties.Resources.macro32x32));
                }

                return RibbonButtons;
            }
            return null;

        } 

        #endregion

        #region Handlers

        /// <summary>
        /// Called when the Ribbon button is invoked.
        /// </summary>
        /// <param name="parameter">Unused.</param>
        /// 
        public void HandleMacroOptions(string macroAlias, List<Field> macroParams)
        {
            string newText = "<umbraco:macro Alias=\"" + macroAlias + "\" runat=\"server\" ";

            if (macroParams != null && macroParams.Any())
            {
                var parms = new Params(macroParams);

                if (WebMatrixHost.ShowDialog("Macro Properties", parms, DialogSize.Medium).GetValueOrDefault())
                {
                    //build <umbraco:macro... string
                    foreach (var Field in macroParams)
                    {
                        switch (Field.DataType)
                        {
                            case "String":
                            case "Int32":
                                TextBox tb = (TextBox)parms.FindName(Field.Alias);
                                newText += Field.Alias + "=\"" + tb.Text + "\" ";
                                break;

                            case "Boolean":
                                CheckBox cb = (CheckBox)parms.FindName(Field.Alias);

                                string umbYesNo = "0";
                                if ((bool)cb.IsChecked) umbYesNo = "1";

                                newText += Field.Alias + "=\"" + umbYesNo + "\" ";
                                break;

                            default:
                                break;
                        }

                    }
                }
            }

            newText += "></umbraco:Macro>";

            // Need to expose the actual command, but for now getting the Editor Guid and the editor command
            InsertTextOverSelection(newText);
        }

        private void HandlePropertyOptions(string propertyAlias)
        {
            string newText = "<umbraco:Item field=\"" + propertyAlias + "\" runat=\"server\" />";

            InsertTextOverSelection(newText);
        }

        private void HandleHTML5Video(object parameter)
        {
            // Get options
            var videoOptions = new VideoOptions
            {
                VideoFile = "big_buck_bunny_trailer_480p_high.mp4",
                VideoWidth = 720,
                VideoHeight = 480,
            };

            if (WebMatrixHost.ShowDialog("Video Options", videoOptions, DialogSize.SizeToContent).GetValueOrDefault())
            {
                // Add custom content
                string newText = string.Format(Html5VideoSnippet, videoOptions.VideoFile, videoOptions.VideoHeight, videoOptions.VideoWidth);

                InsertTextOverSelection(newText);
            }
        }

        private void HandleBreadcrumb(object paramter)
        {
            // Add custom content
            string newText = BreadcrumbSnippet;

            InsertTextOverSelection(newText);
        }

        private void HandleListSubPages(object paramter)
        {
            // Add custom content
            string newText = ListSubPagesSnippet;

            InsertTextOverSelection(newText);
        }

        private void HandleSiteMap(object paramter)
        {
            // Add custom content
            string newText = SiteMapSnippet;

            InsertTextOverSelection(newText);
        }

        private void HandleMacroParameters(object parameter)
        {
            string newText = MacroParametersSnippet;

            InsertTextOverSelection(newText);
        }

        private void HandleInsertMedia(object parameter)
        {
            string newText = InsertMediaSnippet;

            InsertTextOverSelection(newText);
        }

        private void HandleNavigation(object parameter)
        {
            string newText = NavigationSnippet;

            InsertTextOverSelection(newText);
        }

        private void HandlePaging(object parameter)
        {
            string newText = PagingSnippet;

            InsertTextOverSelection(newText);
        }

        private void HandleSelectChildrenByType(object parameter)
        {
            string newText = SelectChildrenByTypeSnippet;

            InsertTextOverSelection(newText);
        }

        private void HandleRelatedLinks(object parameter)
        {
            string newText = RelatedLinksSnippet;

            InsertTextOverSelection(newText);
        }

        private void InsertTextOverSelection(string insertText)
        {
            Guid GroupId = new Guid(GroupIdString);

            var command = WebMatrixHost.HostCommands.GetCommand(GroupId, 10);

            //var container = new Container();
            var container = GetEditor();

            command.Execute(container);

            if (container != null)
            {
                // Paste the snippet into the editor
                var paste = WebMatrixHost.HostCommands.Paste;
                if (paste.CanExecute(insertText))
                {
                    paste.Execute(insertText);
                }
            }
        }

        #endregion

        #region Constants
        private const string GroupIdString = "27a0f541-c86c-4f0b-b436-0b50bf9f7ef8";
        
        private const string Html5VideoSnippet =
        @"<div class=""videocontainer"">
            <h2 class=""videoheader"">HTML5 video</h2>
            <div id=""h264high"">
                <video src=""{0}"" controls preload=""metadata"" height=""{1}"" width=""{2}"">
                    <div class=""sorry"">
                      Your browser cannot play H.264 high profile content with the HTML5 video element.
                    </div>
                </video>
            </div>
        ";

        private const string BreadcrumbSnippet =
            @"<umbraco:Macro  runat=""server"" language=""cshtml"">
            @*
            BREADCRUMB
            =================================
            This snippet makes a breadcrumb of parents using an unordred html list.

            How it works:
            - It uses the Ancestors() method to get all parents and then generates links so the visitor get go back
            - Finally it outputs the name of the current page (without a link)
  
            NOTE: It is safe to remove this comment (anything between @ * * @), the code that generates the list is only the below!
            *@
            @inherits umbraco.MacroEngines.DynamicNodeContext
            <ul>
            @foreach(var level in @Model.Ancestors().Where(""umbracoNaviHide != true""))
            {
                <li><a href=""@level.Url"">@level.Name</a></li>
            }
            <li>@Model.Name</li>
            </ul>
            </umbraco:Macro>
            ";

        private const string ListSubPagesSnippet =
            @"<umbraco:Macro  runat=""server"" language=""cshtml"">
            @*
            LIST SUBPAGES BY LIMIT AND DATETIME
            ===================================
            This snippet shows how easy it is to combine different queries. It lists the children of the currentpage which is
            visible and then grabs a specified number of items sorted by the day they're updated.

            How it works:
            - It uses the Take() method to specify a maximum number of items to output
            - It adds a OrderBy() to sort the items. You can even combine this, for instance OrderBy(""UpdateDate, Name desc"")
  
            NOTE: It is safe to remove this comment (anything between @ * * @), the code that generates the list is only the below!
            *@
            @inherits umbraco.MacroEngines.DynamicNodeContext
            @{ var numberOfItems = 10; }
            <ul>
                @foreach (var item in @Model.Children.Where(""Visible"").OrderBy(""UpdateDate"").Take(numberOfItems)) 
                {
                    <li><a href=""@item.Url"">@item.Name</a></li>
                }
            </ul>
            </umbraco:Macro>
            ";

        private const string SiteMapSnippet =
            @"<umbraco:Macro runat=""server"" language=""cshtml"">
                @*
                SITEMAP
                =================================
                This snippet generates a complete sitemap of all pages that are published and visible (it'll filter out any 
                pages with a property named ""umbracoNaviHide"" that's set to 'true'). It's also a great example on how to make
                helper methods in Razor and how to pass values to your '.Where' filters.

                How to Customize for re-use (only applies to Macros, not if you insert this snippet directly in a template):
                - If you add a Macro Parameter with the alias of ""MaxLevelForSitemap"" which specifies how deep in the hierarchy to traverse 

                How it works:
                - The first line (var maxLevelForSitemap) assigns default values if none is specified via Macro Parameters
                - Next is a helper method 'traverse' which uses recursion to keep making new lists for each level in the sitemap
                - Inside the the 'traverse' method there's an example of using a 'Dictionary' to pass the 'maxLevelForSitemap' to
                  the .Where filter
                - Finally the 'traverse' method is called taking the very top node of the website by calling AncesterOrSelf()

                NOTE: It is safe to remove this comment (anything between @ * * @), the code that generates the list is only the below!
                *@
                @inherits umbraco.MacroEngines.DynamicNodeContext

                @helper traverse(dynamic node){
                var maxLevelForSitemap = String.IsNullOrEmpty(Parameter.MaxLevelForSitemap) ? 4 : int.Parse(Parameter.MaxLevelForSitemap); 

                var values = new Dictionary<string,object>();
                values.Add(""maxLevelForSitemap"", maxLevelForSitemap) ;

                   var items = node.Children.Where(""Visible"").Where(""Level <= maxLevelForSitemap"", values);
                   if (items.Count() > 0) { 
                   <ul>
                            @foreach (var item in items) {
                                <li>
					                <a href=""@item.Url"">@item.Name</a>
					                @traverse(item)
                                </li>
                            }
                   </ul>
                    }
                }
                <div id=""sitemap"">
                    @traverse(@Model.AncestorOrSelf())
                </div>
             </umbraco:Macro>
            ";

        private const string MacroParametersSnippet =
            @"<umbraco:Macro runat=""server"" language=""cshtml"">
                @*
                MACRO PARAMETERS
                ===================================
                This snippet is a very simple example on how to grab values specified via Macro Parameters. Macro Parameters are
                'attributes' that can be added to Macros (doesn't make sense when Razor is used inline) that makes it possible to
                re-use macros for multiple purposes. When you add a Macro Parameter to a Macro, the user can send different values
                to the Macro when it's inserted. Macro Parameters in Razor can be accessed via the Parameter property.

                How it works:
                - In this example it'll output the value specified in a Macro Parameter with the alias 'Who'.
  
                NOTE: It is safe to remove this comment (anything between @ * * @), the code that generates the list is only the below!
                *@

                <h3>Hello @Parameter.Who</h3>
                </umbraco:Macro>
            ";

        private const string InsertMediaSnippet =
            @"<umbraco:Macro runat=""server"" language=""cshtml"">
                @*
                USING MEDIA
                =================================
                This snippet shows two ways of working with referenced media. Media is referenced from a page using a property with
                the type of 'MediaPicker' (or similar for instance MultiNodePicker in uComponents).

                How it works:
                - First we check that there's a property on the current page called 'relatedMedia' and that it has a selected value
                - In the first example we simply needs the path of the media which is stored in the property 'umbracoFile' and the 
                  media is referenced in the property with the alias of 'relatedMedia'. One line is all it takes!
                - In the second example we store the referenced media in a variable as we'd like to get not just the path but also
                  the name of the media for the friendly alt attribute.
  
                NOTE: It is safe to remove this comment (anything between @ * * @), the code that generates the list is only the below!
                *@

                @inherits umbraco.MacroEngines.DynamicNodeContext
                @if (Model.HasProperty(""relatedMedia"") && Model.RelatedMedia != 0) {
	                <p>Simple: <br />
		                <img src='@Model.Media(""relatedMedia"", ""umbracoFile"")' />
	                </p>

	                <p>Advanced: <br />
		                @{
			                var image = @Model.Media(""relatedMedia"");
		                }
		                <img src='@image.UmbracoFile' alt='@image.Name' />
	                </p>
                } else {
	                <p>
		                This page doesn't contain a MediaPicker property with the alias of 'RelatedMedia' 
		                or No image is selected on this page
	                </p>
                }

                </umbraco:Macro>
            ";

        private const string NavigationSnippet = 
            @"<umbraco:Macro runat=""server"" language=""cshtml"">
                @*
                NAVIGATION BY LEVEL
                =================================
                This snippet makes it easy to do navigation based lists! It'll automatically list all children of a page with a certain 
                level in the hierarchy that's published and visible (it'll filter out any pages with a property named ""umbracoNaviHide""
                that's set to 'true'.

                How to Customize for re-use (only applies to Macros, not if you insert this snippet directly in a template):
                - If you add a Macro Parameter with the alias of ""Level"" you can use this macro for both level 1 and level 2 navigations
                - If you add a Macro Parameter with the alias of ""ulClass"" you can specify different css classes for the <UL/> element

                How it works:
                - The first two lines (var level... and var ulClass) assigns default values if none is specified via Macro Parameters
                - Then it finds the correct parent based on the level and assigns it to the 'parent' variable.
                - Then it runs through all the visible children in the foreach loop and outputs a list item
                - Inside the list item it checks if the page added to the list is a parent of the current page. Then it marks it 'selected'

                NOTE: It is safe to remove this comment (anything between @ * * @), the code that generates the list is only the below!
                *@

                @inherits umbraco.MacroEngines.DynamicNodeContext
                @{ 
	                var level = String.IsNullOrEmpty(Parameter.Level) ? 1 : int.Parse(Parameter.Level); 
	                var ulClass = String.IsNullOrEmpty(Parameter.UlClass) ? """" : String.Format("" class=\""{0}\"""", Parameter.UlClass); 
	                var parent = @Model.AncestorOrSelf(level);
	                if (parent != null) {
		                <ul@Html.Raw(ulClass)>
		                @foreach (var item in parent.Children.Where(""Visible"")) {
			                var selected = Array.IndexOf(Model.Path.Split(','), item.Id.ToString()) >= 0 ? "" class=\""selected\"""" : """";
			                <li@Html.Raw(selected)>
				                <a href=""@item.Url"">@item.Name</a>
			                </li>
			                }
		                </ul>
	                }
                }

                </umbraco:Macro>
            ";

        private const string PagingSnippet = 
            @"<umbraco:Macro runat=""server"" language=""cshtml"">
                @*
                HOW TO DO PAGING
                =================================
                This an example of how to do paging of content including a Google style page navigation. You likely want to
                modify the query (first line, assigned to the 'pagesToList' variable) and the output made within the foreach 
                loop (<li> ... </li>)

                How to Customize for re-use (only applies to Macros, not if you insert this snippet directly in a template):
                - You can customize the number of items per page by adding a Macro Parameter with the alias of ""ItemsPerPage""
                - You can customize the labels of previous/next by adding Macro Parameters with the alias of ""PreviousLabel"" and
                  ""NextLabel""

                How it works:
                - The pages to display is added to the variable 'pagesToList'. To change what pages to list, simply update the query
                - The next part assigns the number of items and the previous/next labels using either default values or Macro Parameters
                - Then it's using a bit of math to calculate how many pages and what should currently be displayed
                - In the first <p /> element, a summary is printed. This could likely be removed
                - In the <ul /> the magic happens. Notice how it's using Skip() and Take() to jump to the relevant items and iterate
                  over the number of items to display
                - In the end it added a Google style page navigation (<<Previous 1 2 3 4 Next >>)

                  NOTE: It is safe to remove this comment (anything between @ * * @), the code that generates the list is only the below!
                *@

                @inherits umbraco.MacroEngines.DynamicNodeContext
                @{
                  var pagesToList = @Model.Children;

                  // configuration
                  var itemsPerPage = String.IsNullOrEmpty(Parameter.ItemsPerPage) ? 3 : int.Parse(Parameter.ItemsPerPage);
                  var previousLabel = String.IsNullOrEmpty(Parameter.PreviousLabel) ? ""Previous"" : Parameter.PreviousLabel;
                  var nextLabel = String.IsNullOrEmpty(Parameter.NextLabel) ? ""Next"" : Parameter.NextLabel;

                  // paging calculations
                  var numberOfItems = pagesToList.Count();
                  int currentPage = 1;
                  if (!int.TryParse(HttpContext.Current.Request.QueryString[""Page""], out currentPage)) {
                    currentPage = 1;
                  }
                  currentPage--;
                  var numberOfPages = numberOfItems % itemsPerPage == 0 ? Math.Ceiling((decimal)(numberOfItems / itemsPerPage)) : Math.Ceiling((decimal)(numberOfItems / itemsPerPage))+1; 

                  <p>
                    Total Items: @numberOfItems <br />
                    Items per Page: @itemsPerPage<br />
                    Pages: @numberOfPages;<br />
                    Current Page: @(currentPage)
                  </p>

                  <ul>
                    @foreach(var item in pagesToList.Skip(currentPage*itemsPerPage).Take(itemsPerPage))
                    {
                      <li>@item.Name</li>
                    }
                  </ul>

                  <p class=""pagingPages"">
                    @{
	                // Google style paging links
                    if (currentPage > 0) {
                      <a href=""?page=@(currentPage)"">&laquo; @previousLabel</a>
                    } else {
                      <span class=""pagingDisabled"">&laquo; @previousLabel</span>
                    }
    
                    var Pages = Enumerable.Range(1, (int)numberOfPages);
                    foreach(var number in Pages) {
                      if (number-1 != currentPage) {
                      <a href=""?page=@number"">@number</a>
                      } else {
                      @number
                      }
                      @Html.Raw(""&nbsp&nbsp"");
                    }

                    if (currentPage < Pages.Count()-1) {
                      <a href=""?page=@(currentPage+2)"">@nextLabel &raquo;</a>
                    } else {
                      <span class=""pagingDisabled"">@nextLabel &raquo;</span>
                    }
                  }
                  </p>
                }

                </umbraco:Macro>
            ";

        private const string SelectChildrenByTypeSnippet = 
            @"<umbraco:Macro runat=""server"" language=""cshtml"">
                @*
                LIST CHILDREN BY TYPE
                =================================
                This snippet shows how simple it is to fetch only children of a certain Document Type using Razor. Instead of
                calling .Children, simply call .AliasOfDocumentType (even works in plural for readability)! 
                For instance .Textpage or .Textpages (you can find the alias of your Document Type by editing it in the 
                Settings section).

                NOTE: It is safe to remove this comment (anything between @ * * @), the code that generates the list is only the below!
                *@
                <ul>
                    @foreach (var item in @Model.umbTextpages.Where(""Visible""))
                    {
                    <li><a href=""@item.Url"">@item.Name</a></li>
                }
                </ul>
                </umbraco:Macro>
            ";

        private const string RelatedLinksSnippet = 
            @"<umbraco:Macro runat=""server"" language=""cshtml"">
                @*
                USING RELATED LINKS (AND OTHER XML BASED TYPES)
                ==============================================
                This snippet shows how to work with properties that stores multiple values in XML such as the ""Related Links"" data type.
                When the Razor (or in fact the 'DynamicNode') detected XML, it automatically makes it possible to navigate the xml by
                using the name of the XML elements as properties. Be aware that the first xml element (the container) is always skipped
                and that the properties are case sensitive!

                How it works:
                - First we check if there's a property on the current page (Model) named 'relatedLinks'
                - Then we loop through the XML elements of the property 'RelatedLinks' (ie. all the links)
                - For each link we check if it should be opened in a new window (stored in an XML attribute called 'newwindow' which is
                  automatically translated into a property '.newwindow' by DynamicNode
                - Then we test if the link type is a internal or external link, and if it's an internal link we use the NiceUrl helper
                  method to convert the id of the page to a SEO friendly url
  
                NOTE: It is safe to remove this comment (anything between @ * * @), the code that generates the list is only the below!
                *@

                @inherits umbraco.MacroEngines.DynamicNodeContext

                @{
	                if (Model.HasProperty(""relatedLinks"")) {
	                <ul>
		                @foreach (var link in @Model.RelatedLinks) {
			                string target = link.newwindow == ""1"" ? """" target=\""_blank\"""" : """";
			                <li>
				                @if (link.type == ""internal"") {
					                <a href=""@umbraco.library.NiceUrl(int.Parse(link.link))""@Html.Raw(target)>@link.title</a>
				                } else {
					                <a href=""@link.link""@Html.Raw(target)>@link.title</a>
				                }
			                </li>
		                }
	                </ul>
	                }
                }

                </umbraco:Macro>
            ";

        #endregion

        #region Custom Classes

        /// <summary>
        /// Gets an IEditorExt instance if the editor is in use.
        /// </summary>
        /// <returns>IEditorExt reference.</returns>
        private IEditor GetEditor()
        {
            var workspace = WebMatrixHost.Workspace as IEditorWorkspace;

            if (workspace != null)
            {
                return workspace.CurrentEditor;
            }

            return null;
        }

        //class Container : EditorContainer
        //{
        //    public IEditorExt Editor
        //    {
        //        get;
        //        set;
        //    }
        //}

        public class Macro
        {
            public string Name { get; set; }
            public string Alias { get; set; }
            public List<Field> MacroParams { get; set; }
        }

        public class umbProperty
        {
            public string Name { get; set; }
            public string Alias { get; set; }
        } 

        #endregion
    }
}
