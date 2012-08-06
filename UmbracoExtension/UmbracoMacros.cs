using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlServerCe;

using Microsoft.WebMatrix.Extensibility;
using Microsoft.WebMatrix.Common;
using Microsoft.WebMatrix.Core;
using System.Data.SqlClient;

namespace UmbracoExtension
{
    class UmbracoMacros
    {

        protected SqlCeDataReader MacroList(string dbPath)
        {
            try
            {
                SqlCeConnection conn = new SqlCeConnection("DataSource=" + dbPath + "\\App_Data\\umbraco.sdf");
                conn.Open();
                SqlCeCommand cmd = new SqlCeCommand("select macroAlias, macroName from cmsMacro order by macroName", conn);
                SqlCeDataReader macroRenderings = cmd.ExecuteReader();

                return macroRenderings;
            }
            catch (MissingMethodException)
            {
                return null;
            }
        }

        public IEnumerable<IRibbonButton> GetMacroList(string dbPath)
        {
            //todo:  check for db type (sqlce, sql) then branch

            SqlCeDataReader r = MacroList(dbPath);
            string MacroName = string.Empty;

            if (r != null)
            {
                List<IRibbonButton> RibbonButtons = new List<IRibbonButton>();
                UmbracoExtension u = new UmbracoExtension();

                //IEnumerable<IRibbonButton> RibbonButtons = new IRibbonButton[]
                //{
                while (r.Read())
                {
                    RibbonButtons.Add(new RibbonButton(
                            r["MacroName"].ToString(),
                            new DelegateCommand(u.HandleMacroOptions),
                            "UmbracoMacro",
                            Properties.Resources.macro16x16,
                            Properties.Resources.macro32x32));
                }
                //};

                    return RibbonButtons; 
            }
            return null;
        }


    }
}
