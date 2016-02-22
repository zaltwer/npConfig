using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace neConfig
{
    class ScriptList : ObservableCollection<ScriptData>
    {
    }
    class ScriptData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set
            {
                _Name = value;
                OnPropertyChanged("Name");
            }
        }
        private string _FuncName;
        public string FuncName
        {
            get { return _FuncName; }
            set
            {
                _FuncName = value;
                OnPropertyChanged("FuncName");
            }
        }
        private string _FileName;
        public string FileName
        {
            get { return _FileName; }
            set
            {
                _FileName = value;
                OnPropertyChanged("FileName");
            }
        }
        private ScriptList _Children;
        public ScriptList Children
        {
            get { return _Children; }
            set
            {
                _Children = value;
                OnPropertyChanged("Children");
            }
        }
        private string _assign;
        public string assign
        {
            get { return _assign; }
            set
            {
                _assign = value;
                OnPropertyChanged("assign");
            }
        }
        public int ID { get; set; }
        public bool IsValid { get; set; }
        public BitmapImage HeadImage{ get; set; }
        public ScriptData()
        {
            Name = "";
            assign = "";
            FuncName = "";
            FileName = "";
        }
        public ScriptData(string name)
        {
            Name = name;
            assign = "";
            FuncName = "";
            FileName = "";
        }
    }

    class ScriptInfoList : ObservableCollection<ScriptInfo>
    {
    }
    class ScriptInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        private string _FileName;
        public string FileName
        {
            get { return _FileName; }
            set
            {
                _FileName = value;
                OnPropertyChanged("FileName");
            }
        }
        private string _SRC;
        public string SRC
        {
            get { return _SRC; }
            set
            {
                _SRC = value;
                OnPropertyChanged("SRC");
            }
        }
        private ObservableCollection<string> _ScriptName;
        public ObservableCollection<string> ScriptName
        {
            get { return _ScriptName; }
            set
            {
                _ScriptName = value;
                OnPropertyChanged("ScriptName");
            }
        }
    }

    //セパレータ表示用コンバータ
    [ValueConversion(typeof(string), typeof(string))]
    public class SepConv : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string aaa = (string)value;
            if(aaa == "-")
            {
                aaa = "――――――――――";
            }
            return aaa;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string aaa = (string)value;
            if(aaa == "-")
            {
                aaa = "――――――――――";
            }
            return aaa;
        }
    }
    //セパレータ表示用コンバータ2
    [ValueConversion(typeof(string), typeof(string))]
    public class SepConv2 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string aaa = (string)value;
            if (aaa == "-")
            {
                aaa = "セパレータ";
            }
            return aaa;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string aaa = (string)value;
            if (aaa == "セパレータ")
            {
                aaa = "-";
            }
            return aaa;
        }
    }
}
