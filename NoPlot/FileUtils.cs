using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace NoPlot
{
    public static class FileUtils
    {
        internal static string settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Autodesk\\Revit\\NoPlot.config";

        public static string GetString(string addinName, string setting)
        {
            if (!File.Exists(settingsPath))
                return null;

            XElement xml = XElement.Load(settingsPath);

            // Get an addin matching the addin name
            var addin = xml.Descendants("Addin").Where(a => (string)a.Attribute("Name") == addinName).FirstOrDefault();
            if (addin == null)
                return null;

            XElement _setting = addin.Descendants("Setting").Where(s => (string)s.Attribute("Name") == setting).FirstOrDefault();
            if (_setting == null)
                return null;

            string settingVal = _setting.Value.ToString();
            return settingVal;
        }


        public static bool GetString(string addinName, string setting, out string val)
        {
            val = null;

            val = GetString(addinName, setting);
            if (val == null)
                return false;

            return true;
        }


        public static bool GetInt(string addinName, string setting, out int val)
        {
            val = -1;
            string strval = GetString(addinName, setting);
            if (strval == null)
                return false;

            return int.TryParse(strval, out val);
        }

        public static bool GetDouble(string addinName, string setting, out double val)
        {
            val = double.NaN;
            string strval = GetString(addinName, setting);
            if (strval == null)
                return false;

            return double.TryParse(strval, out val);
        }


        public static bool SetString(string addinName, string setting, string newVal)
        {

            XElement xml = null;

            if (!File.Exists(settingsPath))
            {
                // create a new settings file.
                xml = new XElement("NoPlot");
                xml.Save(settingsPath);
            }

            xml = XElement.Load(settingsPath);

            // Get an addin matching the addin name
            var addins = xml.Descendants("Addin").Where(a => (string)a.Attribute("Name") == addinName).Select(a => a).FirstOrDefault();
            if (addins == null)
            {
                // save the new addin/setting
                var newAddin = new XElement("Addin", new XAttribute("Name", addinName));
                var newSetting = new XElement("Setting", new XAttribute("Name", setting), newVal);
                newAddin.Add(newSetting);
                xml.Add(newAddin);
                xml.Save(settingsPath);
                return true;
            }


            XElement _setting = addins.Descendants("Setting").Where(s => (string)s.Attribute("Name") == setting).FirstOrDefault();
            if (_setting != null)
            {
                _setting.Value = newVal;
                xml.Save(settingsPath);
                return true;
            }


            // Add the missing setting
            _setting = new XElement("Setting", new XAttribute("Name", setting), newVal);
            addins.Add(_setting);
            xml.Save(settingsPath);
            return true;
        }


        public static bool SetInt(string addinName, string setting, int newVal)
        {
            return SetString(addinName, setting, newVal.ToString());
        }

        public static bool SetDouble(string addinName, string setting, double newVal)
        {
            return SetString(addinName, setting, newVal.ToString());
        }
    }
}
