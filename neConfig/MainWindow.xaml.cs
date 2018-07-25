using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Controls.Primitives;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace neConfig
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        //データグリッドデータバインド用
        ICollectionView CV01;

        //割り当て済みショートカット管理リスト
        //KEY：ショートカット　VAL：ID
        private Dictionary<string, int> AllAssign = new Dictionary<string, int>();
        //ショートカット全定義リスト
        //KEY：ID　VAL：機能名
        private Dictionary<int, string> AllName = new Dictionary<int, string>();
        //キー表記変換リスト
        //KEY：KEY列挙体表記　VAL：ネコペ表記
        private Dictionary<string, string> NekoKeyConvDef = new Dictionary<string, string>();

        //スクリプトツリービュー選択中のアイテム
        TreeViewItem SelectedTV01Item = new TreeViewItem();

        const int DefID = 4352;
        int ScriptID = DefID;   //スクリプトショートカット用仮ID
        int IDRangeF, IDRangeT;

        Binding myBinding = new Binding();
        Binding myBinding2 = new Binding();

        private List<string> userlist = new List<string>();

        //猫ペイント本体ディレクトリ
        string NpDir = "";

        //IO系クラス
        neConfigIO necIO = new neConfigIO();

        public MainWindow()
        {
            InitializeComponent();

            #region 起動時チェックなど
            //二重起動確認
            if (System.Diagnostics.Process.GetProcessesByName("neConfig").Length > 1)
            {
                //二重起動禁止
                Close();
                return;
            }
            //ネコペ起動確認
            if (System.Diagnostics.Process.GetProcessesByName("npaint_script").Length > 0)
            {
                //ネコペ起動中は実行禁止
                MessageBox.Show("ネコペイント起動中にこのツールは使用できません\nネコペイントを終了後に再度実行して下さい");
                Close();
                return;
            }
            //ユーザー設定ファイルロード
            UserSettings.LoadSetting();
            //起動ディレクトリ設定
            NpDir = necIO.GetNpDir();
            if (NpDir == null || NpDir == "")
            {
                MessageBox.Show("key.txtが見つかりません。neConfigを終了します", "エラー");
                Close();
                return;
            }
            #endregion
            #region ショートカット設定タブ
            //各種設定取得
            //キー表記変換リスト取得
            necIO.GetDef(NekoKeyConvDef, Properties.Settings.Default.ConvDef);
            //ショートカット全定義リスト取得
            necIO.GetDef(AllName, Properties.Settings.Default.AllName);

            //フィルター追加
            CV01 = CollectionViewSource.GetDefaultView(KeyListGrid.DataContext);
            CV01.Filter = new Predicate<object>(GridFilter1);
            necIO.GetAllDef(KeyList);
            IDRangeF = KeyList.Count();

            //ネコペkey.txt取得
            necIO.ReadNekoKeytxt(NpDir,ScriptID, AllAssign, AllName, KeyList);
            IDRangeT = KeyList.Count();

            Pop01.PlacementTarget = Label1;
            #endregion                  
            #region スクリプト設定タブ
            //ツリービューにルートディレクトリ作成
            SL.Add(new ScriptData("スクリプト"));
            SL[0].Children = new ScriptList();
            SL[0].HeadImage = new BitmapImage(new Uri("Resources/folder.png", UriKind.RelativeOrAbsolute));

            //スクリプト設定取得
            ReadScriptList(NpDir,"user_list.txt");
            ReadScriptList(NpDir,"list.txt");

            //スクリプトディレクトリから全メソッドのリストを作成
            DirectoryInfo dir = new DirectoryInfo(NpDir + "/script/menu/");
            foreach (FileInfo f in dir.GetFiles())
            {
                //拡張子.nas以外は無視
                if (f.Extension != ".nas") continue;
                ScriptInfo SI = new ScriptInfo();
                SI.ScriptName = necIO.ReadScriptFile(NpDir, f.Name);
                SI.FileName = f.Name;
                SI.SRC = File.ReadAllText(NpDir + "/script/menu/" + f.Name,
                    System.Text.Encoding.GetEncoding("Shift_JIS"));
                SIL.Add(SI);
            }
            //ツリービューを一段階展開
            if (TV01.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
            {
                TV01.ItemContainerGenerator.StatusChanged += delegate
                {
                    if (TV01.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                    {
                        TreeViewItem tmpTV = TV01.ItemContainerGenerator.ContainerFromIndex(0) as TreeViewItem;
                        tmpTV.IsExpanded = true;
                    }
                };
            }
            else
            {
                //こっちは多分通らない
                TreeViewItem tmpTV = TV01.ItemContainerGenerator.ContainerFromIndex(0) as TreeViewItem;
                tmpTV.IsExpanded = true;
            }
            #endregion

            //ショートカット入力エリアのバインド切り替え用
            myBinding = BindingOperations.GetBinding(EditListBox, ListBox.ItemsSourceProperty);
            myBinding2.ElementName = "TV01";
            myBinding2.Converter = new AssignConverter2();
            myBinding2.Path = new PropertyPath( "SelectedValue.assign");
            myBinding2.Mode = BindingMode.TwoWay;
            Pop01.PlacementTarget = InpBox;
        }

        /// <summary>
        /// データグリッド用のフィルター
        /// </summary>
        /// <param name="o"></param>
        private bool GridFilter1(object o)
        {
            KeyConfigData KD = (KeyConfigData)o;
            bool Ret = true;
            if (KD != null && Filter01.SelectedValue != null)
            {
                ComboBoxItem aaa = (ComboBoxItem)Filter01.SelectedItem;
                if (Convert.ToString(aaa.Content) == "全て"
                    || KD.Cat == Convert.ToString(aaa.Content))
                {
                    Ret = GridFilter2(o);
                }
                else
                {
                    Ret = false;
                }
            }
            return Ret;
        }
        private bool GridFilter2(object o)
        {
            KeyConfigData KD = o as KeyConfigData;
            bool Ret = true;
            if (KD != null && Filter02.SelectedValue != null)
            {
                ComboBoxItem aaa = (ComboBoxItem)Filter02.SelectedItem;
                if (Convert.ToString(aaa.Content) == "全て")
                {
                    Ret = true;
                }
                else if (Convert.ToString(aaa.Content) == "割り当て済み"
                    && KD.assign != "")
                {
                    Ret = true;
                }
                else if (Convert.ToString(aaa.Content) == "未割り当て"
                    && KD.assign == "")
                {
                    Ret = true;
                }
                else
                {
                    Ret = false;
                }
            }
            return Ret;
        }

        /// <summary>
        /// 猫ペイントのスクリプトリストを取得してショートカット一覧に追加
        /// 書式エラーの行は削除してショートカット設定はその分ずらす。
        /// </summary>
        private void ReadScriptList(string NpDir,string FileName)
        {
            try
            {
                // シフトJISのファイルの読み込み
                string[] lines1 = File.ReadAllLines(NpDir + "/script/menu/" + FileName,
                    System.Text.Encoding.GetEncoding("Shift_JIS"));
                foreach (string line in lines1)
                {
                    string Path;
                    string tmpDiv;
                    int ifrom, ito;
                    ScriptData Data = new ScriptData();

                    //最初の#を検索
                    ito = line.IndexOf("#");
                    if (ito < 0)
                    {
                        //#がない場合全部抽出
                        tmpDiv = line;
                        Data.Name = "";
                        Path = "";
                        //やっぱり無名スクリプトは無視でいいか…
                        DelID(ScriptID);
                        ScriptID++;
                        continue;
                    }
                    else
                    {
                        //最初の#以降をパスとして抽出
                        Path = line.Substring(ito + 1);
                        //一文字目を判定
                        if (Path[0] == '/' || Path.Contains("//"))
                        {
                            //無名ディレクトリ（先頭/）or（連続/）も無視でいいか…
                            Data.Name = "";
                            DelID(ScriptID);
                            ScriptID++;
                            continue;
                        }
                        //最後の/を検索
                        ifrom = Path.LastIndexOf("/");
                        if (ifrom == Path.Length - 1)
                        {
                            Data.Name = "";
                            //無名スクリプト（末尾/）も無視でいいか…
                            DelID(ScriptID);
                            ScriptID++;
                            continue;
                        }
                        else if (ifrom >= 0)
                        {
                            //最後の/以降をスクリプト表示名として抽出
                            Data.Name = Path.Substring(ifrom + 1);
                        }
                        else
                        {
                            //ディレクトリなしなら全てスクリプト名
                            Data.Name = Path;
                        }
                        //#より前の部分の処理
                        if (ito == 0)
                        {
                            //#が先頭の場合
                            tmpDiv = "";
                        }
                        else
                        {
                            //最初の#までを抽出
                            tmpDiv = line.Substring(0, ito);
                        }
                    }
                    //最初の空白を検索
                    ito = tmpDiv.IndexOf(" ");
                    if (Data.Name == "-")
                    {
                        //セパレータにはショートカット割り当てを許可しない
                        //スクリプトが登録されていても無視
                        DelID(ScriptID);
                    }
                    else if (ito < 0)
                    {
                        //空白がない場合全てスクリプトファイル名
                        Data.FileName = tmpDiv;
                        Data.FuncName = "";

                        //先頭＃以外は面倒なので無視
                        if (tmpDiv != "")
                        {
                            DelID(ScriptID);
                            ScriptID++;
                            continue;
                        }
                    }
                    else
                    {
                        //最初の空白までをスクリプトファイル名として抽出
                        Data.FileName = tmpDiv.Substring(0, ito);
                        ifrom = ito + 1;
                        //2番目の空白を検索
                        ito = tmpDiv.IndexOf(" ", ifrom);
                        if (ito < 0)
                        {
                            //空白がない場合全てスクリプト名
                            Data.FuncName = tmpDiv.Substring(ifrom); ;
                            //スクリプト名がない場合は除外
                            if (Data.FuncName == "")
                            {
                                DelID(ScriptID);
                                ScriptID++;
                                continue;
                            }
                        }
                        else
                        {
                            //2番目の空白までをスクリプトファイル名として抽出
                            //2番めの空白以降は無視
                            Data.FuncName = tmpDiv.Substring(ifrom, ito - ifrom);
                        }
                    }
                    ScriptList tmpTree = SL[0].Children;

                    //ショートカット一覧を検索
                    bool Flg = false;
                    foreach (var tmpKD in KeyList)
                    {
                        if (tmpKD.ID == ScriptID)
                        {
                            //登録済みの場合はショートカット名を上書きしてキー割り当てを取得
                            Flg = true;
                            tmpKD.name = Path;
                            Data.assign = tmpKD.assign;
                            AllName[ScriptID] = Path;
                            break;
                        }
                    }
                    //スクリプトファイル名が取得できている場合
                    if (Data.FileName != "")
                    {
                        if (Flg == false)
                        {
                            //リスト未登録の場合は追加する
                            KeyConfigData fd = new KeyConfigData();
                            AllName.Add(ScriptID, Path);
                            fd.ID = ScriptID;
                            fd.name = Path;
                            fd.assign = "";
                            fd.Cat = "スクリプト";
                            KeyList.Add(fd);
                        }
                    }
                    else
                    {
                        //ファイル名が取得できていない場合コメント行かセパレータのはず
                        //なのでショートカットからは削除
                        DelID(ScriptID);
                    }
                    Data.ID = ScriptID;
                    //スクリプト一覧ツリー追加先検索
                    FindTree(ref Path, ref tmpTree);
                    //スクリプト一覧ツリーに追加
                    AddTree(Path, Data, ref tmpTree);
                    ScriptID++;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// /区切りの文字列をツリービュー用階層データに設定
        /// </summary>
        /// <param name="src"></param>
        /// <param name="Data"></param>
        /// <param name="Tree"></param>
        private void AddTree(string src, ScriptData Data, ref ScriptList Tree)
        {
            //srcから最初の/を検索
            int ito = src.IndexOf("/");
            //最初の/までを抽出
            if (ito >= 0)
            {
                ScriptData data = new ScriptData();
                Tree.Add(data);
                data.Name = src.Substring(0, ito);
                data.HeadImage = new BitmapImage(new Uri("Resources/folder.png", UriKind.RelativeOrAbsolute));
                ScriptList xxx = new ScriptList();
                data.Children = xxx;
                AddTree(src.Substring(ito + 1), Data, ref xxx);
            }
            else
            {
                Data.HeadImage = new BitmapImage(new Uri("Resources/script.png", UriKind.RelativeOrAbsolute));
                Tree.Add(Data);
            }
        }

        /// <summary>
        /// ツリービュー用階層データから/区切りの文字列を検索
        /// ヒットした階層と残りの文字列を返す
        /// </summary>
        /// <param name="src"></param>
        /// <param name="destTree"></param>
        private void FindTree(ref string src, ref ScriptList destTree)
        {
            //srcから最初の/を検索
            int ito = src.IndexOf("/");
            //最初の/までを抽出
            if (ito >= 0)
            {
                string Name = src.Substring(0, ito);
                foreach (var aaa in destTree)
                {
                    if (aaa.FileName == "" && aaa.Name == Name)
                    {
                        src = src.Substring(ito + 1);
                        destTree = aaa.Children;
                        FindTree(ref src, ref destTree);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// リスト選択
        /// 一覧で選択した機能に割り当て済みのショートカットは自動で編集リストに反映される
        /// ここでは各種表示の初期化のみ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeyGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (EditListBox.Items.Count > 0)
            {
                //編集リストが０件でない場合先頭行を選択
                EditListBox.SelectedIndex = 0;
            }
            //キー入力BOXを有効にしてフォーカス設定＋初期化
            InpBox.IsEnabled = true;
//            InpBox.Focus();
            InpBox.Text = "";
            //追加ボタンを無効に
            AddButton.IsEnabled = false;
            PopText.Text = "";
            Pop01.IsOpen = false;
        }

        /// <summary>
        /// データグリッド表示カスタマイズ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeyGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // プロパティ名をもとに自動生成する列をカスタマイズします
            switch (e.PropertyName)
            {
                case "name":
                    e.Column.Header = "機能";
                    break;
                case "assign":
                    e.Column.Header = "ショートカットキー";
                    break;
                case "ID":
                    //IDは非表示
                    e.Column.Visibility = System.Windows.Visibility.Hidden;
                    break;
                case "Cat":
                    e.Column.Header = "カテゴリ";
                    e.Column.DisplayIndex = 1;
                    break;

                default:
                    //                    throw new InvalidOperationException();
                    break;
            }
        }

        /// <summary>
        /// ショートカット入力処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InpBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //表示クリア
            InpBox.Text = "";
            PopText.Text = "";
            Pop01.IsOpen = false;
            //修飾キーの処理
            if ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) > 0)
            {
                InpBox.Text = InpBox.Text + "Ctrl + ";
            }
            if ((e.KeyboardDevice.Modifiers & ModifierKeys.Alt) > 0)
            {
                InpBox.Text = InpBox.Text + "Alt + ";
            }
            if ((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) > 0)
            {
                InpBox.Text = InpBox.Text + "Shift + ";
            }
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift
                || e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl
                || e.Key == Key.LeftAlt || e.Key == Key.RightAlt
                || (e.Key == Key.System
                    && (e.SystemKey == Key.LeftShift || e.SystemKey == Key.RightShift
                || e.SystemKey == Key.LeftCtrl || e.SystemKey == Key.RightCtrl
                || e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt))
                )
            {
                //追加ボタンを無効に
                AddButton.IsEnabled = false;
            }
            //修飾キー以外
            else
            {
                string tmp;
                if (e.Key == Key.System)
                {
                    //Altと同時押しの場合などにこっちになる可能性あり
                    tmp = Convert.ToString(e.SystemKey);
                }
                else
                {
                    //普通はこっち
                    tmp = Convert.ToString(e.Key);
                }
                //表示変換が必要なキーを検索
                if (NekoKeyConvDef.ContainsKey(tmp))
                {
                    //表示変換対象なら上書き
                    tmp = (string)NekoKeyConvDef[tmp];
                }
                InpBox.Text += tmp;
                //自分に割り当て済みの場合
                if (EditListBox.Items.Contains(InpBox.Text))
                {
                    PopText.Text = "割り当て済です";
                    Pop01.IsOpen = true;
                    //追加ボタンを無効に
                    AddButton.IsEnabled = false;
                }
                else
                {
                    //割り当て済みリストに存在する場合
                    if (AllAssign.ContainsKey(InpBox.Text))
                    {
                        if (AllAssign[InpBox.Text] >= 4352)
                        {
                            PopText.Text = "【スクリプト】「" + AllName[AllAssign[InpBox.Text]] + "」に割り当て中です";
                        }
                        else
                        {
                            PopText.Text = "「" + AllName[AllAssign[InpBox.Text]] + "」に割り当て中です";
                        }
                        Pop01.IsOpen = true;
                    }
                    //追加ボタンを有効に
                    AddButton.IsEnabled = true;
                }
            }
            //残りのイベントをキャンセル
            e.Handled = true;
        }

        /// <summary>
        /// 割り当てボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (EditListBox.Items.Contains(InpBox.Text))
            {
                InpBox.Text = "";
                //追加ボタンを無効に
                AddButton.IsEnabled = false;
            }
            else
            {
                //同一機能の4件目以降のショートカットは無視されるため確認メッセージを出力
                if (EditListBox.Items.Count >= 3)
                {
                    string sMsg = "同一機能へのキー割り当ては３件までしか認識されませんが割り当てを行いますか？";
                    if (MessageBox.Show(sMsg, "確認", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    {
                        InpBox.Focus();
                        return;
                    }
                }
                if (AllAssign.ContainsKey(InpBox.Text))
                {
                    string sMsg = "";
                    int tmp = AllAssign[InpBox.Text];
                    if (tmp >= 4352)
                    {
                        sMsg = "【スクリプト】「" + AllName[AllAssign[InpBox.Text]] + "」への割り当てを解除して置き換えますか？";
                    }
                    else
                    {
                        sMsg = "「" + AllName[AllAssign[InpBox.Text]] + "」への割り当てを解除して置き換えますか？";
                    }
                    if (MessageBox.Show(sMsg, "ショートカットの置き換え", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    {
                        InpBox.Focus();
                        return;
                    }
                    foreach (var kl in KeyList)
                    {
                        if (kl.ID == tmp)
                        {
                            //元の割り当てを削除
                            kl.DelAssign(InpBox.Text);
                            if (TAB_base.SelectedIndex == 1)
                            {
                                //スクリプト設定タブの場合スクリプトツリーを更新
                                UpdateTree(SL, kl.ID, kl.assign);
                            }
                            break;
                        }
                    }
                }
                ObservableCollection<string> aaa = (ObservableCollection<string>)EditListBox.ItemsSource;
                aaa.Add(InpBox.Text);
                EditListBox.ItemsSource = aaa;
                //タブ固有処理
                if (TAB_base.SelectedIndex == 0)
                {
                    //ショートカット設定タブ
                    //割り当て済みリストを更新
                    KeyConfigData KD = (KeyConfigData)KeyListGrid.SelectedValue;
                    AllAssign[InpBox.Text] = KD.ID;
                    if (KD.ID >= 4352)
                    {
                        //ID4352以降（スクリプト）の場合ツリーを更新
                        UpdateTree(SL, KD.ID, KD.assign);
                    }
                }
                if (TAB_base.SelectedIndex == 1)
                {
                    //スクリプト設定タブ
                    //割り当て済みリストを更新
                    ScriptData KD = TV01.SelectedItem as ScriptData;
                    AllAssign[InpBox.Text] = KD.ID;
                    //ショートカットリストを更新
                    UpdateKD(KeyList, KD.ID, KD.assign);
                }
                InpBox.Text = "";
                PopText.Text = "";
                Pop01.IsOpen = false;
                //追加ボタンを無効に
                AddButton.IsEnabled = false;
                CV01.Refresh();
            }
        }

        /// <summary>
        /// 削除ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DelButton_Click(object sender, RoutedEventArgs e)
        {
            int idx = EditListBox.SelectedIndex;
            if (idx >= 0)
            {
                AllAssign.Remove((string)EditListBox.SelectedValue);
                ObservableCollection<string> aaa = (ObservableCollection<string>)EditListBox.ItemsSource;
                aaa.Remove((string)EditListBox.SelectedValue);
                EditListBox.ItemsSource = aaa;
                //タブ固有処理
                if (TAB_base.SelectedIndex == 0)
                {
                    //ショートカット設定タブ
                    KeyConfigData KD = (KeyConfigData)KeyListGrid.SelectedValue;
                    if (KD.ID >= 4352)
                    {
                        //ID4352以降（スクリプト）の場合ツリーを更新
                        UpdateTree(SL, KD.ID, KD.assign);
                    }
                }
                if (TAB_base.SelectedIndex == 1)
                {
                    //スクリプト設定タブ
                    ScriptData KD = TV01.SelectedItem as ScriptData;
                    //ショートカットリストを更新
                    UpdateKD(KeyList, KD.ID, KD.assign);
                }
                InpBox.Text = "";
                PopText.Text = "";
                Pop01.IsOpen = false;
                CV01.Refresh();
            }
        }

        /// <summary>
        /// カテゴリフィルタ変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Filter01_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CV01 != null && CV01 != null)
                CV01.Refresh();
        }

        /// <summary>
        /// 割り当てフィルタ変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Filter02_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CV01 != null && CV01 != null)
                CV01.Refresh();
        }

        /// <summary>
        /// 編集リスト選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EditListBox.SelectedIndex >= 0)
            {
                DelButton.IsEnabled = true;
            }
            else
            {
                DelButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// ショートカット保存ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void save_Click(object sender, RoutedEventArgs e)
        {
            //ネコペ起動確認
            if (System.Diagnostics.Process.GetProcessesByName("npaint_script").Length > 0)
            {
                //ネコペ起動中は実行禁止
                MessageBox.Show("ネコペイント起動中は保存できません\nネコペイントを終了後に再度保存して下さい");
                return;
            }
            if (MessageBox.Show("ショートカット設定・スクリプト設定を保存しますか？", "保存", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                //保存前にID振り直し
                RefreshScript();
                //Key.txt書き込み
                necIO.WriteNekoKeytxt(NpDir, KeyList);
                //出力用のスクリプト設定を作成
                userlist.Clear();
                string tmp = "";
                Makelisttxt(SL[0].Children, tmp);
                //userlist.txt書き込み
                necIO.WriteNekoListtxt(NpDir, userlist);

                MessageBox.Show("ショートカット設定・スクリプト設定が保存されました");
            }
        }

        private void save_Key_Click(object sender, RoutedEventArgs e)
        {
            //ネコペ起動確認
            if (System.Diagnostics.Process.GetProcessesByName("npaint_script").Length > 0)
            {
                //ネコペ起動中は実行禁止
                MessageBox.Show("ネコペイント起動中は保存できません\nネコペイントを終了後に再度保存して下さい");
                return;
            }
            if (MessageBox.Show("ショートカット設定を保存しますか？", "保存", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                //Key.txt書き込み
                necIO.WriteNekoKeytxt(NpDir, KeyList);
                MessageBox.Show("ショートカット設定が保存されました");
            }
        }

        /// <summary>
        /// 選択されたアイテムの保持
        /// D&Dの元ネタになります
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeViewItemSelected(object sender, RoutedEventArgs e)
        {
            //ドラッグ開始アイテム保持
            SelectedTV01Item = e.OriginalSource as TreeViewItem;
            if (SelectedTV01Item.Header == SL[0])
            {
                //ルート選択時は変更不可に
                NameBox.IsEnabled = false;
                FileNameCmb.IsEnabled = false;
                ScriptNameCmb.IsEnabled = false;
                AddScript.IsEnabled = true;
                AddSep.IsEnabled = true;
                AddDir.IsEnabled = true;
                DelScript.IsEnabled = false;
            }
            else
            {
                NameBox.IsEnabled = true;
                FileNameCmb.IsEnabled = true;
                ScriptNameCmb.IsEnabled = true;
                AddScript.IsEnabled = true;
                AddSep.IsEnabled = true;
                AddDir.IsEnabled = true;
                DelScript.IsEnabled = true;
            }
            //スクリプトが選択された場合はコンボボックスを同期する
            ScriptData temp = SelectedTV01Item.Header as ScriptData;
            if (temp.Children == null)
            {
                //セパレータの場合
                if (temp.Name == "-")
                {
                    NameBox.IsEnabled = false;
                    FileNameCmb.IsEnabled = false;
                    ScriptNameCmb.IsEnabled = false;
                }
                else
                {
                    int i = 0;
                    foreach (ScriptInfo item in FileNameCmb.Items)
                    {
                        if (item.FileName == temp.FileName)
                        {
                            //ファイル名が一致するレコードを選択
                            FileNameCmb.SelectedIndex = i;
                            //ポップアップ非表示（未実装）
                            //スクリプト名コンボボックスを有効に
                            ScriptNameCmb.IsEnabled = true;
                            break;
                        }
                        i++;
                    }
                    if (i == FileNameCmb.Items.Count)
                    {
                        //一致レコードがなかった場合
                        //スクリプト名コンボボックスを無効に
                        FileNameCmb.SelectedIndex = -1;
                        ScriptNameCmb.IsEnabled = false;
                        //ポップアップ表示（未実装）
                    }
                    i = 0;
                    foreach (string item in ScriptNameCmb.Items)
                    {
                        if (item == temp.FuncName)
                        {
                            //スクリプト名が一致するレコードを選択
                            ScriptNameCmb.SelectedIndex = i;
                            break;
                        }
                        i++;
                    }
                    if (i == ScriptNameCmb.Items.Count)
                    {
                        //一致レコードがなかった場合
                        ScriptNameCmb.SelectedIndex = -1;
                        //ポップアップ表示（未実装）
                    }
                    if (EditListBox.Items.Count > 0)
                    {
                        //編集リストが０件でない場合先頭行を選択
                        EditListBox.SelectedIndex = 0;
                    }
                    FileNameCmb.IsEnabled = true;
                    //キー入力BOXを有効に
                    InpBox.IsEnabled = true;
                }
            }
            else
            {
                //ディレクトリを選択した場合はコンボボックスをクリアして無効に
                ScriptNameCmb.SelectedIndex = -1;
                FileNameCmb.SelectedIndex = -1;
                ScriptNameCmb.IsEnabled = false;
                FileNameCmb.IsEnabled = false;
                //キー入力BOXを無効に
                InpBox.IsEnabled = false;
            }
            //キー入力BOXを初期化
            InpBox.Text = "";
            //追加ボタンを無効に
            AddButton.IsEnabled = false;
            PopText.Text = "";
            Pop01.IsOpen = false;
        }

        //親要素リスト取得関数はネットから丸パクリ
        private IEnumerable<ItemsControl> GetGCetontainerFromElementList(ItemsControl topItemsControl, DependencyObject buttomControl)
        {
            ItemsControl tmp = topItemsControl;
            while (tmp != null)
            {
                tmp = ItemsControl.ContainerFromElement(tmp, buttomControl) as ItemsControl;
                //自分を含めないようにしてみる
                if (tmp != null && tmp != buttomControl)
                {
                    yield return tmp;
                }
            }
        }

        /// <summary>
        /// ドラッグ開始処理
        /// 本来はMouseDown時の座標から一定量移動後にドラッグ開始とするべきだが
        /// ツリービューのオブジェクト上のMouseDownがそのままでは拾えないのと
        /// 自分宛てのD&Dは無視するため、即ドラッグ開始としている
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TV01_MouseMove(object sender, MouseEventArgs e)
        {
            TreeView srcTV = sender as TreeView;
            //ルートディレクトリはドラッグ不可
            if (srcTV != null && srcTV.SelectedItem != null && e.LeftButton == MouseButtonState.Pressed
                && SelectedTV01Item.Header != SL[0])
            {
                //D&D開始
                DragDrop.DoDragDrop(srcTV, srcTV.SelectedItem, DragDropEffects.Move);
            }
        }

        private void TextBlock_MouseMove(object sender, MouseEventArgs e)
        {
            //ルートディレクトリはドラッグ不可
            if (TV01 != null && TV01.SelectedItem != null && e.LeftButton == MouseButtonState.Pressed
                && SelectedTV01Item.Header != SL[0])
            {
                //D&D開始
                DragDrop.DoDragDrop(TV01, TV01.SelectedItem, DragDropEffects.Move);
            }

        }

        /// <summary>
        /// ドロップ処理
        /// 自分宛てと自分の子孫要素へのD&Dは無視
        /// 外部からのD&Dも無視。無効にしたいけど制御めんどいのでしない
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TV01_Drop(object sender, DragEventArgs e)
        {
            //ドラッグ終了処理
            e.Handled = true;
            e.Effects = DragDropEffects.None;

            //操作対象のツリービュー
            var srcTV = sender as TreeView;

            //ドラッグ元ノードの具
            var srcData = srcTV.SelectedItem as ScriptData;

            //ドラッグ元ノードの親を取得
            TreeViewItem SrcParent = GetGCetontainerFromElementList(srcTV, SelectedTV01Item).LastOrDefault() as TreeViewItem;

            //ドロップ先オブジェクト（TreeVewItem内部のコントロールしか取れない）
            DependencyObject destControl = e.OriginalSource as DependencyObject;
            if (destControl != null && destControl.DependencyObjectType.Name == "TextBlock")
            {
                //ドロップ先の強調を解除
                TextBlock txWk = (TextBlock)destControl;
                txWk.TextDecorations = TextDecorationCollectionConverter.ConvertFromString("none");
                txWk.FontWeight = FontWeights.Normal;

                //ドロップ先オブジェクトのパスリストを取得
                var DestPath = GetGCetontainerFromElementList(srcTV, destControl);
                //ドロップ先オブジェクトのパスにドラッグ元がいないかチェック
                foreach (var Path in DestPath)
                {
                    if (Path == SelectedTV01Item)
                    {
                        //自分ならなにもしないで終了
                        return;
                    }
                }
                //ドロップ先オブジェクトが格納されたTreeVewItemをドロップ先ノードとして取得
                TreeViewItem destTVItem = DestPath.LastOrDefault() as TreeViewItem;
                if (destTVItem != null)
                {
                    //ドロップ先ノードの具を取得
                    ScriptData destData = destTVItem.Header as ScriptData;
                    //自分にドロップした場合は何もしない
                    if (srcData != destData)
                    {
                        //ドラッグ元ノードの親の具を取得
                        ScriptData srcParentData = SrcParent.Header as ScriptData;
//                        if (destData == SL[0]) //ドロップ先がルート以外なら中に入れないようにしてみる
//                        if (destData.Children != null) //フォルダなら中に入れるよう修正
                        if (destData.Children != null && destTVItem.IsExpanded == true) //フォルダかつ展開中なら中に入れるよう修正
                        {
                            //座標取得テスト（未使用）
                            Point pt = txWk.PointToScreen(new Point(0.0d, 0.0d));
                            Point ptM = txWk.PointToScreen(new Point(0.0d, 0.0d));
                            Point ptD = e.GetPosition(txWk);
                            //ドロップ先がディレクトリなら先頭に追加
                            if (SrcParent == destTVItem)
                            {
                                //同じフォルダなら移動
                                destData.Children.Move(srcParentData.Children.IndexOf(srcData), 0);
                            }else{
                                //別フォルダの場合追加＋削除
                                destData.Children.Insert(0, srcData);
                                srcParentData.Children.Remove(srcData);
                            }
                        }
                        else
                        {
                            //ディレクトリでなければ親ディレクトリのドロップ位置に挿入
                            TreeViewItem destParent = GetGCetontainerFromElementList(srcTV, destTVItem).LastOrDefault() as TreeViewItem;
                            ScriptData destParentData = destParent.Header as ScriptData;
                            srcParentData.Children.Remove(srcData);
//                            destParentData.Children.Insert(destParentData.Children.IndexOf(destData), srcData);
                            //前挿入から後ろに追加に変更
                            destParentData.Children.Insert(destParentData.Children.IndexOf(destData)+1, srcData);
                        }
                    }
                }
                //ID振り直し
                RefreshScript();
            }
        }

        /// <summary>
        /// 新規スクリプト追加ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddScript_Click(object sender, RoutedEventArgs e)
        {
            if (TV01.SelectedItem != null)
            {
                ScriptData Data = new ScriptData();
                //使いまわすので呼び出し元判定
                if (sender == AddScript)
                {
                    //スクリプト追加ボタンの場合
                    Data.HeadImage = new BitmapImage(new Uri("Resources/script.png", UriKind.RelativeOrAbsolute));
                    Data.Name = "新しいスクリプト";
                    Data.ID = ScriptID;
                    AllName.Add(Data.ID, Data.Name);
                    ScriptID++;
                }
                else if (sender == AddSep)
                {
                    //セパレータ追加ボタンの場合
                    Data.HeadImage = new BitmapImage(new Uri("Resources/script.png", UriKind.RelativeOrAbsolute));
                    Data.Name = "-";
                    Data.ID = ScriptID;
                    ScriptID++;
                }
                else
                {
                    //ディレクトリ追加ボタンの場合
                    Data.Name = "新しいフォルダ";
                    Data.HeadImage = new BitmapImage(new Uri("Resources/folder.png", UriKind.RelativeOrAbsolute));
                    Data.Children = new ScriptList();
                }
                ScriptData Target = TV01.SelectedItem as ScriptData;
                if (Target.Children != null)
                {
                    //選択中のノードがディレクトリならそのまま追加
                    Target.Children.Add(Data);
                    //追加したノードを選択
                    SelectedTV01Item.IsExpanded = true;
                    if(SelectedTV01Item.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                    {
                        TreeViewItem src = SelectedTV01Item.ItemContainerGenerator.ContainerFromIndex(Target.Children.Count - 1) as TreeViewItem;
                        src.IsSelected = true;
                    }
                    else
                    {
                        SelectedTV01Item.ItemContainerGenerator.StatusChanged += delegate
                        {
                            if (SelectedTV01Item.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                            {
                                TreeViewItem src = SelectedTV01Item.ItemContainerGenerator.ContainerFromIndex(Target.Children.Count - 1) as TreeViewItem;
                                if(src != null) src.IsSelected = true;
                            }
                        };
                    }
                }
                else
                {
                    //ディレクトリ以外なら親に追加
                    TreeViewItem destParent = GetGCetontainerFromElementList(TV01, SelectedTV01Item).LastOrDefault() as TreeViewItem;
                    ScriptData destParentData = destParent.Header as ScriptData;
                    destParentData.Children.Insert(destParentData.Children.IndexOf(Target), Data);
                    //追加したノードを選択
                    TreeViewItem src = destParent.ItemContainerGenerator.ContainerFromIndex(destParentData.Children.IndexOf(Data)) as TreeViewItem;
                    src.IsSelected = true;
                }
                //ID振り直し
                RefreshScript();
            }
        }

        /// <summary>
        /// スクリプト削除ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DelScript_Click(object sender, RoutedEventArgs e)
        {
            //ルートは削除不可
            if (TV01.SelectedItem != null && SelectedTV01Item.Header != SL[0])
            {
                //親を取得
                TreeViewItem destParent = GetGCetontainerFromElementList(TV01, SelectedTV01Item).LastOrDefault() as TreeViewItem;
                ScriptData destParentData = destParent.Header as ScriptData;
                //選択中のノードを削除
                destParentData.Children.Remove((ScriptData)TV01.SelectedItem);
                //ID振り直し
                RefreshScript();
            }
        }

        /// <summary>
        /// スクリプトファイル名選択コンボボックス
        /// 表示は重ねてるテキストボックスに
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileNameCmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ScriptInfo tmp = FileNameCmb.SelectedItem as ScriptInfo;
            if (tmp != null)
            {
                FileNameBox.Text = tmp.FileName;
                SrcBox.ScrollToHome();
                if (FileNameCmb.IsKeyboardFocusWithin == true)
                {
                    //ユーザーがコンボボックスを変更した場合はクリア
                    FuncNameBox.Text = "";
                    ScriptNameCmb.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// スクリプト選択コンボボックス
        /// 表示は重ねてるテキストボックスに
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScriptNameCmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string tmp = ScriptNameCmb.SelectedItem as string;
            if (tmp != null && ScriptNameCmb.IsKeyboardFocusWithin == true)
            {
                FuncNameBox.Text = tmp;
            }
        }

        /// <summary>
        /// タブ切り替え時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TAB_base_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //子オブジェクトの変更に反応しないようにする
            if (e.OriginalSource == TAB_base)
            {
                //キー設定タブが選択されたらスクリプト設定を再読み込み
                if (TAB_base.SelectedIndex == 0)
                {
                    //スクリプトリセット処理呼び出し
                    RefreshScript();
                    //入力系のバインドを切り替え
                    EditListBox.SetBinding(ListBox.ItemsSourceProperty, myBinding);
                    if (KeyListGrid.SelectedValue == null)
                    {
                        InpBox.IsEnabled = false;
                    }
                }
                //スクリプト設定タブ
                if (TAB_base.SelectedIndex == 1)
                {
                    //入力系のバインドを切り替え
                    EditListBox.SetBinding(ListBox.ItemsSourceProperty, myBinding2);
                    if (TV01.SelectedItem == null)
                    {
                        InpBox.IsEnabled = false;
                        AddScript.IsEnabled = false;
                        AddSep.IsEnabled = false;
                        AddDir.IsEnabled = false;
                        DelScript.IsEnabled = false;
                        NameBox.IsEnabled = false;
                    }
                    else
                    {
                        AddScript.IsEnabled = true;
                        AddSep.IsEnabled = true;
                        AddDir.IsEnabled = true;
                        DelScript.IsEnabled = true;
                        NameBox.IsEnabled = true;
                        if (SelectedTV01Item.Header == SL[0])
                        {
                            //ルート選択時は削除不可
                            DelScript.IsEnabled = false;
                        }
                        else
                        {
                            DelScript.IsEnabled = true;
                        }
                    }
                }
                //追加ボタンを無効に
                AddButton.IsEnabled = false;
                //キー入力系をクリア
                InpBox.Text = "";
                PopText.Text = "";
                Pop01.IsOpen = false;
            }
        }

        /// <summary>
        /// 各種キー設定リストから指定したIDのデータを削除
        /// AllAssign,AllName,KeyListが対象
        /// </summary>
        /// <param name="ID"></param>
        private void DelID(int ID)
        {
            //キー設定リストからスクリプト分を削除。ID重複はない前提なので削除は1回のみ
            foreach (var tmp in KeyList)
            {
                //指定IDなら削除
                if (tmp.ID == ID)
                {
                    KeyList.Remove(tmp);
                    break;
                }
            }
            //割り当て済みリストから削除。ID重複ありなので指定ID以外を退避して戻す
            Dictionary<string, int> tmpDic = new Dictionary<string, int>();
            foreach (var Assign in AllAssign)
            {
                //データ件数分ループして指定ID以外のレコードをコピー
                if (Assign.Value != ID)
                {
                    tmpDic.Add(Assign.Key, Assign.Value);
                }
            }
            //戻す
            AllAssign = tmpDic;
            //ショートカット表名リストから削除。IDがキーなので一発削除
            AllName.Remove(ID);
        }

        /// <summary>
        ///スクリプト設定読み込み処理
        ///スクリプトリストを全件読み込み、ショートカットリストに設定
        ///IDは振り直し
        /// </summary>
        /// <param name="SL"></param>
        /// <param name="tmpPath"></param>
        private void SetList(ScriptList SL, string tmpPath)
        {
            foreach (var tmpSL in SL)
            {
                if (tmpSL.Children != null)
                {
                    SetList(tmpSL.Children, tmpPath + tmpSL.Name + "/");
                }
                else if (tmpSL.Name != "-")
                {
                    KeyConfigData tmpKD = new KeyConfigData();
                    tmpKD.Cat = ("スクリプト");
                    tmpKD.name = tmpPath + tmpSL.Name;
                    tmpKD.ID = ScriptID;
                    tmpKD.assign = tmpSL.assign;
                    tmpSL.ID = ScriptID;
                    ScriptID++;
                    KeyList.Add(tmpKD);
                    AllName.Add(tmpKD.ID, tmpKD.name);
                    List<string> tmpDiv = new List<string>();
                    tmpKD.DivAssign(tmpDiv);
                    foreach (string div in tmpDiv)
                    {
                        //割り当て済みリストに追加
                        AllAssign.Add(div, tmpSL.ID);
                    }
                }
                else
                {
                    //セパレータも一行にカウントされるのでIDだけはインクリメント
                    tmpSL.ID = ScriptID;
                    ScriptID++;
                }
            }
        }

        /// <summary>
        /// スクリプトツリー、各種リストのスクリプト部分をID振り直し
        /// </summary>
        private void RefreshScript()
        {
            //キー設定リストからスクリプト分を削除
            for (int i = KeyList.Count - 1; i >= 0; i--)
            {
                //末尾からループしてID4352以降なら削除
                if (KeyList[i].ID >= DefID)
                {
                    KeyList.RemoveAt(i);
                }
            }
            Dictionary<string, int> tmpDic = new Dictionary<string, int>();
            foreach (var Assign in AllAssign)
            {
                //データ件数分ループしてID4352未満のレコードをコピー
                if (Assign.Value < DefID)
                {
                    tmpDic.Add(Assign.Key, Assign.Value);
                }
            }
            //戻す
            AllAssign = tmpDic;
            for (int i = AllName.Count - 1; i >= 0; i--)
            {
                //最大でデータ件数分ループしてID4352以上のレコードを削除
                int j = AllName.Keys.Max();
                if (j >= DefID)
                {
                    AllName.Remove(j);
                }
                else
                {
                    //IDの最大値が4352未満になったら終了
                    break;
                }
            }
            //スクリプトID初期化
            ScriptID = DefID;
            //スクリプトのキー割り当てをスクリプトリストから再設定
            string tmpPath = "";
            SetList(SL[0].Children, tmpPath);
        }

        /// <summary>
        /// スクリプトリストツリーからファイル書き込み用のリスト作成
        /// </summary>
        /// <param name="SL"></param>
        /// <param name="tmpPath"></param>
        private void Makelisttxt(ScriptList SL, string tmpPath)
        {
            foreach (var tmpSL in SL)
            {
                if (tmpSL.Children != null)
                {
                    Makelisttxt(tmpSL.Children, tmpPath + tmpSL.Name + "/");
                }
                else
                {
                    string tmp = "";
                    if (tmpSL.FileName == "")
                    {
                        tmp = "#" + tmpPath + tmpSL.Name;
                    }
                    else
                    {
                        tmp = tmpSL.FileName + " " + tmpSL.FuncName + " #" + tmpPath + tmpSL.Name;
                    }
                    userlist.Add(tmp);
                }
            }
        }

        /// <summary>
        /// スクリプトツリーの指定IDのキー割り当て更新
        /// キー設定タブでの更新は自動でツリーに反映されないためこれを使う
        /// </summary>
        /// <param name="SL"></param>
        /// <param name="ID"></param>
        /// <param name="assign"></param>
        private void UpdateTree(ScriptList SL, int ID, string assign)
        {
            foreach (var tmpSL in SL)
            {
                if (tmpSL.ID == ID)
                {
                    tmpSL.assign = assign;
                    break;
                }
                else if (tmpSL.Children != null)
                {
                    UpdateTree(tmpSL.Children, ID, assign);
                }
            }
        }
        /// <summary>
        /// キーコンフィグリストの指定IDのキー割り当て更新
        /// スクリプト設定タブでの更新は自動でキーコンフィグリストに反映されないためこれを使う
        /// </summary>
        /// <param name="KD"></param>
        /// <param name="ID"></param>
        /// <param name="assign"></param>
        private void UpdateKD(KeyConfigList KD, int ID, string assign)
        {
            foreach (var tmpKD in KD)
            {
                if (tmpKD.ID == ID)
                {
                    tmpKD.assign = assign;
                    break;
                }
            }
        }

        private void NameBox_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ScriptData temp = SelectedTV01Item.Header as ScriptData;
            if (temp.Children == null) if (NameBox.Text == "")
            {
                MessageBox.Show("表示名は空にできません");
                NameBox.Focus();
                e.Handled = true;
            }
            else if (NameBox.Text.Contains("/"))
            {
                MessageBox.Show("表示名に「/」は使用できません");
                NameBox.Focus();
                e.Handled = true;
            }
            else if (temp.Children == null
                &&(NameBox.Text == "-" || NameBox.Text == "セパレータ"))
            {
                //ディレクトリ以外なら強制的に半角スペースを追加
                NameBox.Text += " ";
            }
        }

        private void TextBlock_DragEnter(object sender, DragEventArgs e)
        {
            //カーソル下のテキストを強調表示
            TextBlock txWk = (TextBlock)sender;

            txWk.TextDecorations = TextDecorations.Underline;
//            txWk.FontWeight = FontWeights.Bold; //太字は見づらいので却下
        }

        private void TextBlock_DragLeave(object sender, DragEventArgs e)
        {
            //強調表示解除
            TextBlock txWk = (TextBlock)sender;
            txWk.TextDecorations = TextDecorationCollectionConverter.ConvertFromString("none");
            txWk.FontWeight = FontWeights.Normal;
        }
    }
}
