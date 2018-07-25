using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Documents;

namespace neConfig
{
    class neConfigIO
    {

        /// <summary>
        /// ネコペのディレクトリを取得
        /// 初回起動時や存在しない場合入力させる
        /// </summary>
        /// <param name="Dir"></param>
        public string GetNpDir()
        {
            bool ChkFlg = true;
            string Dir = UserSettings.Instance.NpDir;
            if (Dir == "" || Dir == null)
            {
                //初回起動時
                MessageBox.Show("ネコペイント本体の場所を指定してください","起動時設定");
                ChkFlg = false;
            }
            else
            {
                DirectoryInfo dirChk = new DirectoryInfo(Dir + "\\config");
                if (dirChk.Exists == true)
                {
//                    var aaa = dirChk.GetFiles("npaint_script.exe");
                    var aaa = dirChk.GetFiles("key.txt");
                    if (aaa.Length == 0)
                    {
//                        MessageBox.Show("ネコペイント本体が見つかりません。\nネコペイント本体の場所を指定して下さい", "警告");
                        MessageBox.Show("ネコペイント設定ファイルが見つかりません。\nネコペイント本体の場所を指定して下さい", "警告");
                        ChkFlg = false;
                    }
                }
                else
                {
                    MessageBox.Show("ネコペイント設定ファイルが見つかりません。\nネコペイント本体の場所を指定して下さい", "警告");
                    ChkFlg = false;
                }
            }
            if (ChkFlg == false)
            {
//                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
//                dlg.FileName = "";
//                dlg.Filter = "npaint_script.exe|npaint_script.exe";
//                dlg.Title = "ネコペイント本体の場所を指定して下さい";

                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                fbd.Description = "ネコペイント本体の場所を指定して下さい";
                fbd.ShowNewFolderButton = false;

                //ダイアログを表示する
                Nullable<System.Windows.Forms.DialogResult> result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    //key.txtの存在確認
                    if (System.IO.File.Exists(fbd.SelectedPath + "\\config\\key.txt"))
                    {
                        //key.txtが取得できた場合、指定されたディレクトリを保存
                        Dir = fbd.SelectedPath;
                        UserSettings.Instance.NpDir = Dir;
                        UserSettings.SaveSetting();
                    }
                }
                else
                {
                    Dir = "";
                }
            }
            return (Dir);
        }
        
        /// <summary>
        /// アプリケーション設定からショートカットキー全定義取得
        /// データグリッドの一覧作成元になります
        /// </summary>
        /// <param name="KeyConvDef"></param>
        public void GetAllDef(KeyConfigList KL)
        {
            StringCollection src = Properties.Settings.Default.AllKeyDef;
            //ショートカットキー全定義取得
            foreach (string str in src)
            {
                KeyConfigData fd = new KeyConfigData();
                //最初の空白を検索
                int ito = str.IndexOf(" ");
                //最初の空白までをカテゴリとして抽出
                fd.Cat = str.Substring(0, ito);
                int ifrom = ito + 1;
                ito = str.IndexOf(" ", ifrom);
                //次の空白までをIDとして抽出
                fd.ID = Convert.ToInt32(str.Substring(ifrom, ito - ifrom));
                ifrom = ito + 1;
                //残りを機能名として設定
                fd.name = str.Substring(ifrom);
                KL.Add(fd);
            }
        }

        /// <summary>
        /// 猫ペイントのkey.txtを取得
        /// 取得したキー割り当てを一覧から探して設定
        /// 一覧にない場合（4352以降のスクリプトショートカット）は追加
        /// </summary>
        public void ReadNekoKeytxt(string NpDir,int ScriptID, Dictionary<string, int> Assigned, Dictionary<int, string> AllName, KeyConfigList KL)
        {
            try
            {
                //重複定義回避用
                List<int> dupChk = new List<int>();
                // シフトJISのファイルの読み込み
                string[] lines1 = File.ReadAllLines(NpDir + "\\config\\key.txt",
                    System.Text.Encoding.GetEncoding("Shift_JIS"));
                foreach (string line in lines1)
                {
                    int ID;
                    string KEY;
                    string name = "";
                    List<string> tmpDiv = new List<string>();

                    //最初の空白を検索
                    int ito = line.IndexOf(" ");
                    //最初の空白までをIDとして抽出
                    ID = Convert.ToInt32(line.Substring(0, ito));
                    //重複チェック
                    if(dupChk.Contains(ID))
                    {
                        //重複していたら二件目以降を無視
                        continue;
                    }
                    dupChk.Add(ID);
                    //最初の{を検索
                    int ifrom = ito + 1;
                    ito = line.IndexOf("{", ifrom);
                    if (ito > 0)
                    {
                        name = line.Substring(ifrom, ito - ifrom);
                        //{が発見できた場合(ショートカット設定済み)
                        //{までを機能名として設定
                        ifrom = ito;
                        //最後の}を検索
                        ito = line.LastIndexOf("}");
                        if (ito > ifrom)
                        {
                            //{から}までを取得。前後にスペースを付加
                            KEY = " " + line.Substring(ifrom, ito - ifrom + 1) + " ";
                            //作成済みの一覧からIDを検索
                            bool Flg = false;
                            foreach (var fd in KL)
                            {
                                if (fd.ID == ID)
                                {
                                    //IDの一致するレコードにショートカット設定
                                    fd.assign = KEY;
                                    //分割後の割り当てを取得
                                    fd.DivAssign(tmpDiv);
                                    //重複回避用一時領域
                                    List<string> tmp = new List<string>();
                                    foreach (string div in tmpDiv)
                                    {
                                        if(Assigned.ContainsKey(div) != true)
                                        {
                                            //割り当て済みリストに追加
                                            Assigned.Add(div, ID);
                                            tmp.Add(div);
                                        }
                                    }
                                    if (tmp.Count > 0)
                                    {
                                        fd.CatAssign(tmp);
                                    }
                                    else
                                    {
                                        fd.assign = "";
                                    }
                                    Flg = true;
                                    break;
                                }
                            }
                            if (Flg == false)
                            {
                                AllName.Add(ID, name);
                                KeyConfigData fd = new KeyConfigData();
                                fd.ID = ID;
                                fd.name = name;
                                fd.assign = KEY;
                                fd.Cat = "スクリプト";
                                KL.Add(fd);
                                fd.DivAssign(tmpDiv);
                                foreach (string div in tmpDiv)
                                {
                                    //割り当て済みリストに追加
                                    Assigned.Add(div, ID);
                                }
                            }
                        }
                    }
                    else
                    {
                        name = line.Substring(ifrom);
                        //作成済みの一覧からIDを検索
                        bool Flg = false;
                        foreach (var fd in KL)
                        {
                            if (fd.ID == ID)
                            {
                                Flg = true;
                                break;
                            }
                        }
                        if (Flg == false)
                        {
                            AllName.Add(ID, name);
                            KeyConfigData fd = new KeyConfigData();
                            fd.ID = ID;
                            fd.name = name;
                            fd.Cat = "スクリプト";
                            KL.Add(fd);
                        }
                    }

                }
            }

            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// 猫ペイントのkey.txtを書き込み
        /// </summary>
        /// <param name="KL"></param>
        public void WriteNekoKeytxt(string NpDir,KeyConfigList KL)
        {
            try
            {
                //ファイルにテキストを書き出し
                using (StreamWriter w = new StreamWriter(NpDir + "/config/key.txt"
                    , false, System.Text.Encoding.GetEncoding("Shift_JIS")))
                {
                    foreach (var tmp in KL)
                    {
                        //ネコぺkey.txtに入ってなくてデフォルトでアサインされてる機能への対応
                        if ((tmp.ID >= 790 && tmp.ID <= 793) || tmp.ID == 3334)
                        {
                            //ピクセル単位の移動と全タブ表示切替に割当がない場合、key.txtには書き込まない
                            if (tmp.assign == "")
                            {
                                continue;
                            }
                        }
                        string str = Convert.ToString(tmp.ID) + " " + tmp.name + tmp.assign;
                        w.WriteLine(str);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// 猫ペイントのスクリプトリストを書き込み
        /// 全スクリプトをuser_list.txtに書き込み、list.txtは空にする
        /// </summary>
        /// <param name="KL"></param>
        public void WriteNekoListtxt(string NpDir, List<string> tmpList)
        {
            try
            {
                //ファイルにテキストを書き出し
                using (StreamWriter w = new StreamWriter(NpDir + "/script/menu/user_list.txt"
                    , false, System.Text.Encoding.GetEncoding("Shift_JIS")))
                {
                    foreach (var tmp in tmpList)
                    {
                        w.WriteLine(tmp);
                    }
                }
                //ファイルにテキストを書き出し
                using (StreamWriter w2 = new StreamWriter(NpDir + "/script/menu/list.txt"
                    , false, System.Text.Encoding.GetEncoding("Shift_JIS")))
                {
                    w2.Write("");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// 設定取得汎用
        /// </summary>
        /// <param name="dest"></param>
        public void GetDef(Dictionary<string, string> dest, StringCollection src)
        {
            //キー表記変換定義取得
            int div = 0;
            string key, val;
            foreach (string str in src)
            {
                div = str.IndexOf(' ');
                if (div < 0)
                {
                    continue;
                }
                key = str.Substring(0, div);
                val = str.Substring(div + 1);
                dest.Add(key, val);
            }
        }
        public void GetDef(Dictionary<string, int> dest, StringCollection src)
        {
            //キー表記変換定義取得
            int div = 0;
            string key, val;
            foreach (string str in src)
            {
                div = str.IndexOf(' ');
                if (div < 0)
                {
                    continue;
                }
                key = str.Substring(0, div);
                val = str.Substring(div + 1);
                dest.Add(key, Convert.ToInt32(val));
            }
        }
        public void GetDef(Dictionary<int, string> dest, StringCollection src)
        {
            //キー表記変換定義取得
            int div = 0;
            string key, val;
            foreach (string str in src)
            {
                div = str.IndexOf(' ');
                if (div < 0)
                {
                    continue;
                }
                key = str.Substring(0, div);
                val = str.Substring(div + 1);
                dest.Add(Convert.ToInt32(key), val);
            }
        }

        /// <summary>
        /// nasファイルを読み込み使用可能メソッド一覧を作成
        /// </summary>
        /// <param name="FileName"></param>
        public ObservableCollection<string> ReadScriptFile(string NpDir, string FileName)
        {
            ObservableCollection<string> Ret = new ObservableCollection<string>();
            try
            {
                // シフトJISのファイルの読み込み
                string[] lines1 = File.ReadAllLines(NpDir + "/script/menu/" + FileName,
                    System.Text.Encoding.GetEncoding("Shift_JIS"));
                foreach (string line in lines1)
                {
                    //void *() に一致する行以外無視
                    string tmpDiv;
                    string Name;
                    int idx;

                    //コメントを除外
                    idx = line.IndexOf("//");
                    if (idx < 0)
                    {
                        //#がない場合全部抽出
                        tmpDiv = line;
                    }
                    else
                    {
                        //コメント部以外を抽出
                        tmpDiv = line.Substring(0, idx);
                    }
                    tmpDiv = tmpDiv.Trim();

                    //先頭が"void"でなければ無視
                    idx = tmpDiv.IndexOf("void", StringComparison.CurrentCultureIgnoreCase);
                    if (idx != 0) continue;

                    //空白がなければ無視
                    idx = tmpDiv.IndexOf(" ");
                    if (idx < 0) continue;

                    tmpDiv = tmpDiv.Substring(idx);

                    //最初の()を検索
                    idx = tmpDiv.IndexOf("()");
                    if (idx < 0) continue;

                    Name = tmpDiv.Substring(1, idx - 1);

                    Ret.Add(Name);
                }
                return Ret;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
}
