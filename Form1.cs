using System;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
using OpenCvSharp.Blob;

namespace object_tracking
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        CvCapture capture;
        IplImage src;
        IplImage background;
        IplImage copy;
        IplImage bin;
        IplImage ball;

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                capture = CvCapture.FromFile("../../basketball.mp4");
                background = new IplImage("../../background.jpg", LoadMode.AnyColor);
            }
            catch
            {
                timer1.Enabled = false;
            }
        }

        int frame_count = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            frame_count++;
            label1.Text = frame_count.ToString() + "/" + capture.FrameCount.ToString();
            src = capture.QueryFrame();

            if (frame_count != capture.FrameCount)
            {
                if (copy == null)
                    copy = new IplImage(src.Size, BitDepth.U8, src.NChannels);
                Cv.Copy(src, copy);

                IplImage nobg = RemoveBackground(copy, background);
                IplImage output = TrackAndDrawBall(nobg);

                pictureBoxIpl1.ImageIpl = output;
            }
            else
            {
                frame_count = 0;
                capture = CvCapture.FromFile("../../basketball.mp4");
            }
        }

        public IplImage Binary(IplImage input, int threshold)
        {
            bin = new IplImage(input.Size, BitDepth.U8, 1);
            Cv.CvtColor(input, bin, ColorConversion.BgrToGray); // 그레이스케일
            Cv.Smooth(bin, bin, SmoothType.Blur, 9); // 블러
            Cv.Threshold(bin, bin, threshold, 255, ThresholdType.Binary); // 이진화

            return bin;
        }

        public IplImage RemoveBackground(IplImage input, IplImage background)
        {
            // 배경이미지와 영상의 차이 계산
            IplImage diff = new IplImage(input.Size, BitDepth.U8, 3);
            Cv.AbsDiff(input, background, diff);

            // 배경과 농구공 분리
            IplImage bin = Binary(diff, 100);

            // 모폴로지 연산을 사용하여 노이즈 제거
            IplConvKernel element = new IplConvKernel(8, 8, 1, 1, ElementShape.Ellipse);
            Cv.MorphologyEx(bin, bin, bin, element, MorphologyOperation.Open, 3);

            // 영상과 이진화된 농구공 추출 영상 비트연산하여 배경제거
            Mat input1 = new Mat(input);
            Mat input2 = new Mat(bin);
            Mat bitwise = new Mat();

            Cv2.BitwiseAnd(input1, input2.CvtColor(ColorConversion.GrayToBgr), bitwise);

            return bitwise.ToIplImage();
        }

        public IplImage TrackAndDrawBall(IplImage input)
        {
            // Blob 라벨링하여 농구공을 추적
            ball = new IplImage(input.Size, BitDepth.U8, 1);
            Cv.CvtColor(input, ball, ColorConversion.BgrToGray);            
            CvBlobs blobs = new CvBlobs();
            blobs.Label(ball);

            IplImage result = copy.Clone();

            foreach (var pair in blobs)
            {
                CvBlob blob = pair.Value;

                // 중심 좌표 계산
                double centerX = blob.Centroid.X;
                double centerY = blob.Centroid.Y;

                // double 형식의 좌표를 int로 변환
                int intCenterX = (int)Math.Round(centerX);
                int intCenterY = (int)Math.Round(centerY);

                // 중심 좌표에 파란 원 그리기
                Cv.Circle(result, new CvPoint(intCenterX, intCenterY), 10, CvColor.Blue, -1); // 반지름 10인 파란 원 그리기

                // 중심 좌표 옆에 "ball" 텍스트 그리기
                Cv.PutText(result, "ball", new CvPoint(intCenterX + 20, intCenterY), new CvFont(FontFace.HersheyComplex, 1, 1), CvColor.Red);
            }

            return result;
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Cv.ReleaseImage(src);
            Cv.ReleaseImage(copy);
            Cv.ReleaseImage(bin);
            Cv.ReleaseImage(background);
        }
    }
}
