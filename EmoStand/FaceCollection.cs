using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace EmoStand
{

    public class FaceInfo
    {
        public string Emotion { get; set; }
        public double Value { get; set; }
        public WriteableBitmap Image { get; set; }
    }

    public class FaceCollection
    {
        public ObservableCollection<FaceInfo> Collection = new ObservableCollection<FaceInfo>();

        public void AddFace(EmoCollectionRecord e, WriteableBitmap Img)
        {
            if (Collection.Count > 10) Collection.RemoveAt(0);
            Collection.Add(new FaceInfo() { Emotion = e.Emotion, Value = e.Value, Image = Img });
        }
    }
}
