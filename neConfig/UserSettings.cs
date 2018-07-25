using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Runtime.Serialization;

namespace neConfig
{
    [DataContract]
    public class UserSettings
    {

        //猫ペイント本体ディレクトリ
        [DataMember]
        public string NpDir { get; set; }

        private static UserSettings _instance;
        public static UserSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new UserSettings();
                return _instance;
            }
            set { _instance = value; }
        }

        public static void SaveSetting()
        {
            string appPath = System.Windows.Forms.Application.StartupPath;
            appPath = @"UserSetting.config";

            DataContractSerializer ds = new DataContractSerializer(typeof(UserSettings));
            //Xmlの改行・インデント設定
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "\t";
            XmlWriter xw = XmlWriter.Create(appPath, settings);
            //シリアル化して書き込む
            ds.WriteObject(xw, Instance);
            xw.Close();
        }
        public static void LoadSetting()
        {
            string appPath = System.Windows.Forms.Application.StartupPath;
            appPath = @"UserSetting.config";
            if (System.IO.File.Exists(appPath))
            {
                //ファイルが存在する場合
                DataContractSerializer ds = new DataContractSerializer(typeof(UserSettings));
                XmlReader xr = XmlReader.Create(appPath);
                //XMLファイルから読み込み、逆シリアル化する
                Instance = (UserSettings)ds.ReadObject(xr);
                xr.Close();
            }
        }
    }
}
