﻿using Microsoft.VisualBasic.FileIO;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Shadowsocks.Controller
{

    public static class I18N
    {
        private static readonly string I18N_FILE = "i18n.csv";

        private static Dictionary<string, string> _strings = new Dictionary<string, string>();

        private static void Init(string res, string locale)
        {
            using (TextFieldParser csvParser = new TextFieldParser(new StringReader(res)))
            {
                csvParser.SetDelimiters(",");

                // search language index
                string[] localeNames = csvParser.ReadFields();

                int enIndex = 0;
                int targetIndex = -1;

                for (int i = 0; i < localeNames.Length; i++)
                {
                    if (localeNames[i] == "en")
                        enIndex = i;
                    if (localeNames[i] == locale)
                        targetIndex = i;
                }

                // Fallback to same language with different region
                if (targetIndex == -1)
                {
                    string localeNoRegion = locale.Split('-')[0];
                    for (int i = 0; i < localeNames.Length; i++)
                    {
                        if (localeNames[i].Split('-')[0] == localeNoRegion)
                            targetIndex = i;
                    }
                    Logging.Info($"Using {localeNames[targetIndex]} translation for {locale}");
                }

                // Still not found, exit
                if (targetIndex == -1 || enIndex == targetIndex)
                {
                    Logging.Info($"Translation for {locale} not found");
                    return;
                }

                // read translation lines
                while (!csvParser.EndOfData)
                {
                    string[] translations = csvParser.ReadFields();
                    string source = translations[enIndex];
                    string translation = translations[targetIndex];

                    // source string or translation empty
                    if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(translation)) continue;
                    // line start with comment
                    if (translations[0].TrimStart(' ')[0] == '#') continue;

                    _strings[source] = translation;
                }
            }
        }

        static I18N()
        {
            string i18n;
            if (!File.Exists(I18N_FILE))
            {
                i18n = Resources.i18n_csv;
                File.WriteAllText(I18N_FILE, i18n, Encoding.UTF8);
            }
            else
            {
                i18n = File.ReadAllText(I18N_FILE, Encoding.UTF8);
            }
            Logging.Info("Current language is: " + CultureInfo.CurrentCulture.Name);
            Init(i18n, CultureInfo.CurrentCulture.Name);
        }

        public static string GetString(string key, params object[] args)
        {
            return string.Format(_strings.TryGetValue(key.Trim(), out var value) ? value : key, args);
        }

        public static void TranslateForm(Form c)
        {
            if (c == null) return;
            c.Text = GetString(c.Text);
            foreach (var item in ViewUtils.GetChildControls<Control>(c))
            {
                if (item == null) continue;
                item.Text = GetString(item.Text);
            }
            TranslateMenu(c.Menu);
        }
        public static void TranslateMenu(Menu m)
        {
            if (m == null) return;
            foreach (var item in ViewUtils.GetMenuItems(m))
            {
                if (item == null) continue;
                item.Text = GetString(item.Text);
            }
        }
    }
}
