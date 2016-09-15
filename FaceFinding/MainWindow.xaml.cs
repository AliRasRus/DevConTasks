using System;
using System.Collections.Generic;
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

using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.IO;
using System.Windows.Forms;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;

namespace FaceFinding
{
    public struct FileFaces
    {
        public string FilePath { get; set; }
        public Face[] Faces    { get; set; }
    }

    public struct GroupRequestBody
    {
        public Guid[] FaceIds { get; set; }
    }

    public struct GroupRequestResponce
    {
        public object[][] Groups   { get; set; }
        public Guid[] MessyGroup { get; set; }
    }

    public struct SimilarRequestBody
    {
        public Guid FaceId    { get; set; }
        public Guid[] FaceIds { get; set; }
    }

    public struct SimilarRequestResponce
    {
        public Guid FaceId { get; set; }
        public float Confidence { get; set; }
    }

    public struct EmotionRequestBody
    {
        public Uri Url { get; set; }
    }

    public struct EmotionRequestResponce
    {
        public string EmotionString { get; set; }
        public float Score { get; set; }
        public int OriginalIndex { get; set; }
    }

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string faceSubKey = "588401451cf04f9bb3a766d993d8812a";
        private readonly string emotSubKey = "6176a7effe1e494495a7c4244c47b68c";
        private readonly IFaceServiceClient faceServiceClient = new FaceServiceClient("588401451cf04f9bb3a766d993d8812a");
        //private readonly EmotionServiceClient emotServiceClient = new EmotionServiceClient("6176a7effe1e494495a7c4244c47b68c");

        private int faceCount;

        private FileFaces mainHeroFace;
        private List<FileFaces> cadrFileFaces;

        public MainWindow()
        {
            InitializeComponent();
            mainHeroFace = new FileFaces();
            cadrFileFaces = new List<FileFaces>();
            faceCount = 0;
        }

        private async Task<Face[]> UploadAndDetectFaces(string imageFilePath)
        {
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    var faces = await faceServiceClient.DetectAsync(imageFileStream);
                    //var faceRects = faces.Select(face => face.FaceRectangle);
                    //return faceRects.ToArray();
                    return faces.ToArray();
                }
            }
            catch (Exception)
            {
                return new Face[0];
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDlg = new OpenFileDialog();
            openDlg.Filter = "jpg | *.jpg";
            
            if (openDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            string filePath = openDlg.FileName;

            Title = "Detecting MaiHero...";

            Uri fileUri = new Uri(filePath);
            BitmapImage bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            MainHeroPhoto.Source = bitmapSource;
            Face[] faces = await UploadAndDetectFaces(filePath);

            //рисуем прямоугольник
            mainHeroFace.FilePath = filePath;
            mainHeroFace.Faces = faces;
            var faceRect = faces[0].FaceRectangle;

            DrawingVisual visual = new DrawingVisual();
            DrawingContext drawingContext = visual.RenderOpen();
            drawingContext.DrawImage(bitmapSource,
                new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
            double dpi = bitmapSource.DpiX;
            double resizeFactor = 96 / dpi;

            drawingContext.DrawRectangle(
                Brushes.Transparent,
                new Pen(Brushes.Red, 2),
                new Rect(
                    faceRect.Left * resizeFactor,
                    faceRect.Top * resizeFactor,
                    faceRect.Width * resizeFactor,
                    faceRect.Height * resizeFactor
                    )
            );
            
            drawingContext.Close();
            RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                (int)(bitmapSource.PixelWidth * resizeFactor),
                (int)(bitmapSource.PixelHeight * resizeFactor),
                96,
                96,
                PixelFormats.Pbgra32);

            faceWithRectBitmap.Render(visual);
            MainHeroPhoto.Source = faceWithRectBitmap;
        
            Title = String.Format("MainHero detection finished.");
        }

        private async void BrowseCadrButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog openDlg = new FolderBrowserDialog();
            openDlg.RootFolder = Environment.SpecialFolder.MyComputer;

            if (openDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            string[] filePaths = Directory.GetFiles(openDlg.SelectedPath);
            cadrFileFaces.Clear();
            faceCount = 0;

            if (filePaths.Length > 0)
            {
                foreach (string filePath in filePaths)
                {
                    if (filePath.Contains(".jpg") || filePath.Contains(".JPG"))
                    {
                        Title = "Detecting cadr " + filePath;

                        Uri fileUri = new Uri(filePath);
                        BitmapImage bitmapSource = new BitmapImage();

                        bitmapSource.BeginInit();
                        bitmapSource.CacheOption = BitmapCacheOption.None;
                        bitmapSource.UriSource = fileUri;
                        bitmapSource.EndInit();

                        VideoCadrPhoto.Source = bitmapSource;
                        Face[] faces = await UploadAndDetectFaces(filePath);
                        faceCount += faces.Length;
                        if (faces.Length > 0)
                        {
                            cadrFileFaces.Add(new FileFaces() { FilePath = filePath, Faces = faces });
                        }

                        System.Threading.Thread.Sleep(1000);
                    }
                }

                Title = String.Format("Detection Finished. {0} face(s) detected", faceCount);
            }
        }

        private async void BrowseButton_Copy1_Click(object sender, RoutedEventArgs e)
        {
            if (mainHeroFace.Faces == null || mainHeroFace.Faces.Length == 0 || cadrFileFaces.Count == 0)
            {
                Title = "Пока нечего анализировать";
                return;
            }

            //1.Сгруппируем лица, полученные при анализе раскадровки фильма
            object[] groups = await groupCadrFaces();
            if (groups == null || groups.Length==0)
            {
                Title = "Группирование кадров по лицам не произошло!!!";
                return;
            }
                
            //2.Проверяем наличие главного героя в полученных группах
            Guid[] mainHeroIds = await mainHeroIdent(groups);

            //3.Находим соответствующие кадры с главным героем и анализируем его эмоции
            EmotionRequestResponce[] res = await mainHeroEmotions(mainHeroIds);
            string resultString = "";
            foreach (EmotionRequestResponce em in res)
            {
                resultString += em.EmotionString + $"({em.Score})->";
            }
            System.Windows.MessageBox.Show(resultString,"Изменение эмоций главного героя");
        }

        private async Task<object[]> groupCadrFaces() //группировка лиц из фильма по схожести
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", faceSubKey);

            GroupRequestBody bodyReq = new GroupRequestBody();
            bodyReq.FaceIds = new Guid[faceCount];
            int i = 0;
            foreach(FileFaces ff in cadrFileFaces)
            {
                foreach (Face face in ff.Faces)
                {
                    bodyReq.FaceIds[i++] = face.FaceId;
                }
            }
            string reqString = JsonConvert.SerializeObject(bodyReq);

            HttpRequestMessage textRequest = new HttpRequestMessage(HttpMethod.Post, @"https://api.projectoxford.ai/face/v1.0/group?");
            textRequest.Content = new StringContent(reqString, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage resp = await httpClient.SendAsync(textRequest);

            if (resp.IsSuccessStatusCode)
            {
                string responseContent = "";

                if (resp.Content != null)
                    responseContent = await resp.Content.ReadAsStringAsync();

                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    GroupRequestResponce grRes = JsonConvert.DeserializeObject<GroupRequestResponce>(responseContent);
                    return grRes.Groups.ToArray();
                }
            }
            
            return null;
        }

        private async Task<Guid[]> mainHeroIdent(object[] faceIdGroups) //поиск соответствия лица главного героя группам лиц
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", faceSubKey);

            List<Guid> res = new List<Guid>();

            for(int i = 0; i < faceIdGroups.Length; i++) 
            {
                object[] faceIds = (object[])faceIdGroups[i];

                SimilarRequestBody bodyReq = new SimilarRequestBody();
                bodyReq.FaceId = mainHeroFace.Faces[0].FaceId;
                bodyReq.FaceIds = new Guid[faceIds.Length];
                for (int j = 0; j < faceIds.Length; j++)
                {
                    bodyReq.FaceIds[j] = new Guid(faceIds[j].ToString());
                }

                string reqString = JsonConvert.SerializeObject(bodyReq);

                HttpRequestMessage textRequest = new HttpRequestMessage(HttpMethod.Post, @"https://api.projectoxford.ai/face/v1.0/findsimilars?");
                textRequest.Content = new StringContent(reqString, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage resp = await httpClient.SendAsync(textRequest);
                if (resp.IsSuccessStatusCode)
                {
                    string responseContent = "";

                    if (resp.Content != null)
                        responseContent = await resp.Content.ReadAsStringAsync();

                    if (!string.IsNullOrWhiteSpace(responseContent))
                    {
                        SimilarRequestResponce[] smRes = JsonConvert.DeserializeObject<SimilarRequestResponce[]>(responseContent);
                        foreach (SimilarRequestResponce sReq in smRes)
                        {
                            if (sReq.Confidence > 0.7)
                                res.Add(sReq.FaceId);
                        }
                    }
                }
            }
            
            return res.ToArray();
        }

        private async Task<EmotionRequestResponce[]> mainHeroEmotions(Guid[] mainHeroIds)
        {
            //HttpClient httpClient = new HttpClient();
            //httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", emotSubKey);

            System.Collections.Hashtable mainHeroCadrs = new System.Collections.Hashtable();

            foreach (FileFaces fFile in cadrFileFaces)
            {
                foreach (Face f in fFile.Faces)
                {
                    if (mainHeroIds.Contains(f.FaceId))
                        mainHeroCadrs.Add(fFile.FilePath, f.FaceRectangle);
                }
            }

            var eNum = mainHeroCadrs.GetEnumerator();
            EmotionServiceClient emotServiceClient = new EmotionServiceClient("6176a7effe1e494495a7c4244c47b68c");
            EmotionRequestResponce[] mainHeroEmotions = new EmotionRequestResponce[mainHeroCadrs.Count];
            int i = 0;
            while (eNum.MoveNext())
            {
                Emotion[] emotionResult;
                using (Stream imageFileStream = File.OpenRead(eNum.Key.ToString()))
                {
                    emotionResult = await emotServiceClient.RecognizeAsync(imageFileStream);
                    foreach (Emotion em in emotionResult)
                    {
                        if (em.FaceRectangle.Left == ((FaceRectangle)eNum.Value).Left 
                            && em.FaceRectangle.Top == ((FaceRectangle)eNum.Value).Top
                            && em.FaceRectangle.Width == ((FaceRectangle)eNum.Value).Width
                            && em.FaceRectangle.Height == ((FaceRectangle)eNum.Value).Height)
                        {
                            EmotionRequestResponce emResp = new EmotionRequestResponce();
                            emResp.EmotionString = "";
                            emResp.Score = 0;

                            if (em.Scores.Anger >= emResp.Score)
                            {
                                emResp.EmotionString = "Angry";
                                emResp.Score = em.Scores.Anger;
                            }
                            if (em.Scores.Contempt >= emResp.Score)
                            {
                                emResp.EmotionString = "Contempt";
                                emResp.Score = em.Scores.Contempt;
                            }
                            if (em.Scores.Disgust >= emResp.Score)
                            {
                                emResp.EmotionString = "Disgust";
                                emResp.Score = em.Scores.Disgust;
                            }
                            if (em.Scores.Fear >= emResp.Score)
                            {
                                emResp.EmotionString = "Fear";
                                emResp.Score = em.Scores.Fear;
                            }
                            if (em.Scores.Happiness >= emResp.Score)
                            {
                                emResp.EmotionString = "Happy";
                                emResp.Score = em.Scores.Happiness;
                            }
                            if (em.Scores.Neutral >= emResp.Score)
                            {
                                emResp.EmotionString = "Neutral";
                                emResp.Score = em.Scores.Neutral;
                            }
                            if (em.Scores.Sadness >= emResp.Score)
                            {
                                emResp.EmotionString = "Sad";
                                emResp.Score = em.Scores.Sadness;
                            }
                            if (em.Scores.Surprise >= emResp.Score)
                            {
                                emResp.EmotionString = "Surprise";
                                emResp.Score = em.Scores.Surprise;
                            }

                            mainHeroEmotions[i++] = emResp;
                        }
                    }
                }
                
                /////???Не смог передать локальный файл для обработки
                //EmotionRequestBody bodyReq = new EmotionRequestBody();
                //bodyReq.Url = new Uri(eNum.Key.ToString(), UriKind.Absolute);
                //string reqString = JsonConvert.SerializeObject(bodyReq);
                //string rect = string.Format("{0},{1},{2},{3}", ((FaceRectangle)eNum.Value).Left, ((FaceRectangle)eNum.Value).Top, ((FaceRectangle)eNum.Value).Width, ((FaceRectangle)eNum.Value).Height);

                //HttpRequestMessage textRequest = new HttpRequestMessage(HttpMethod.Post, $"https://api.projectoxford.ai/emotion/v1.0/recognize?faceRectangles={rect}");
                //textRequest.Content = new StringContent(reqString, System.Text.Encoding.UTF8, "application/json");

                //HttpResponseMessage resp = await httpClient.SendAsync(textRequest);
                //if (resp.IsSuccessStatusCode)
                //{
                //    string responseContent = "";

                //    if (resp.Content != null)
                //        responseContent = await resp.Content.ReadAsStringAsync();

                //    if (!string.IsNullOrWhiteSpace(responseContent))
                //    {

                //    }
                //}
                //else
                //{
                //    if (resp.Content != null && resp.Content.Headers.ContentType.MediaType.Contains("application/json"))
                //    {
                //        string errorString = await resp.Content.ReadAsStringAsync();
                //        return false;
                //    }
                //}
            }
            return mainHeroEmotions;
        }
    }
}
