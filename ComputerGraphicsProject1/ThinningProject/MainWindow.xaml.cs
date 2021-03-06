﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

namespace ThinningProject
{
    public enum Col
    {
        BLACK = 0,
        White = 255
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WriteableBitmap OryginalBitmap { get; set; }
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();

            op.Title = "Select a picture";
            op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
                "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                "Portable Network Graphic (*.png)|*.png";
            // op.Multiselect = true;
            if (op.ShowDialog() == true)
            {

                string path = op.FileName;
                string name = op.SafeFileName;
                {
                    try
                    {
                        var bitmapImage = new BitmapImage(new Uri(path));
                        OryginalBitmap = new WriteableBitmap(bitmapImage);
                        OryginalImage.Source = OryginalBitmap;
                        var clone = new WriteableBitmap(OryginalBitmap);
                        ThinedImage.Source = clone;
                    }
                    catch
                    {
                        MessageBox.Show("Please choose correct image");
                    }
                }
            }
        }

        private void SavePicture_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Title = "Select folder to save to";
            sfd.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
                "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                "Portable Network Graphic (*.png)|*.png";
            if (sfd.ShowDialog() == true)
            {
                var clone = ((WriteableBitmap)ThinedImage.Source).Clone();
                if (sfd.FileName != string.Empty)
                {
                    using (FileStream stream5 = new FileStream(sfd.FileName, FileMode.Create))
                    {
                        PngBitmapEncoder encoder5 = new PngBitmapEncoder();
                        encoder5.Frames.Add(BitmapFrame.Create(clone));
                        encoder5.Save(stream5);
                        stream5.Close();
                    }
                }
            }
        }

        private void KMMButton_Click(object sender, RoutedEventArgs e)
        {
            var stopwatch = Stopwatch.StartNew();
            var preProcessedOryginal = PreProcessing();
            //ThinedImage.Source = preProcessedOryginal;
            WriteableBitmap bitmap = new WriteableBitmap(preProcessedOryginal);
            var result = KMM(bitmap);
            ThinedImage.Source = result;
            stopwatch.Stop();
            Console.WriteLine("Whole computations " + stopwatch.ElapsedMilliseconds);
        }


        private WriteableBitmap PreProcessing()
        {
            var stopwatch = Stopwatch.StartNew();
            var fingerBitmap = new WriteableBitmap(OryginalBitmap);
            int stride = fingerBitmap.PixelWidth * 4;
            int size = fingerBitmap.PixelHeight * stride;
            byte[] pixels = new byte[size];
            var gaussianSmothingFilter = CreateGaussianSmothingFilter(2);
            fingerBitmap = ConvolutionFunction(fingerBitmap, gaussianSmothingFilter);
            fingerBitmap = ToGrayScale(fingerBitmap);
            fingerBitmap = ErodeGrayscaleImage(fingerBitmap, 100);
            int threshold = CalculateThreshold(fingerBitmap, 0);
            fingerBitmap = ComputeThresholdImage(fingerBitmap, threshold);
            stopwatch.Stop();
            Console.WriteLine("Preprocessing " + stopwatch.ElapsedMilliseconds);
            return fingerBitmap;
        }

        private void K3MButton_Click(object sender, RoutedEventArgs e)
        {
            var stopwatch = Stopwatch.StartNew();
            var preProcessedOryginal = PreProcessing();
            //ThinedImage.Source = preProcessedOryginal;
            WriteableBitmap bitmap = new WriteableBitmap(preProcessedOryginal);
            var result = K3M(bitmap);
            ThinedImage.Source = result;
            stopwatch.Stop();
            Console.WriteLine("Whole computations " + stopwatch.ElapsedMilliseconds);
        }

        private WriteableBitmap K3M(WriteableBitmap input)
        {
            var height = input.PixelHeight;
            var width = input.PixelWidth;
            var coord = new Coord(height, width);
            var pixels = new byte[coord.Size];
            input.CopyPixels(pixels, coord.Stride, 0);

            int removedPixelsCounter = 0;
            //1. Black pixels = 1, white pixels = 0
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var index = coord.Get(x, y);
                    if (pixels[index] == 0)
                        pixels[index] = 1;
                    else
                    {
                        pixels[index] = 0;
                    }

                }
            }
            var mask = GetMask();
            do
            {
                removedPixelsCounter = 0;
                //PHASE 0 - marking borders
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        var index = coord.Get(x, y);
                        if (pixels[index] != 0 && IsPixelInPhase(mask, pixels, coord, x, y,0))
                        {
                            pixels[index] = 2;
                        }
                    }
                }
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        var index = coord.Get(x, y);
                        if (pixels[index] == 2 && IsPixelInPhase(mask, pixels, coord, x, y, 1))
                        {
                            pixels[index] = 0;
                            removedPixelsCounter++;
                        }
                    }
                }
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        var index = coord.Get(x, y);
                        if (pixels[index] == 2 && IsPixelInPhase(mask, pixels, coord, x, y, 2))
                        {
                            pixels[index] = 0;
                            removedPixelsCounter++;
                        }
                    }
                }
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        var index = coord.Get(x, y);
                        if (pixels[index] == 2 && IsPixelInPhase(mask, pixels, coord, x, y, 3))
                        {
                            pixels[index] = 0;
                            removedPixelsCounter++;
                        }
                    }
                }
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        var index = coord.Get(x, y);
                        if (pixels[index] == 2 && IsPixelInPhase(mask, pixels, coord, x, y, 4))
                        {
                            pixels[index] = 0;
                            removedPixelsCounter++;
                        }
                    }
                }
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        var index = coord.Get(x, y);
                        if (pixels[index] == 2 && IsPixelInPhase(mask, pixels, coord, x, y, 5))
                        {
                            pixels[index] = 0;
                            removedPixelsCounter++;
                        }
                    }
                }
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        var index = coord.Get(x, y);
                        if (pixels[index] == 2)
                        {
                            pixels[index] = 1;
                        }
                    }
                }
            } while (removedPixelsCounter != 0);

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    var index = coord.Get(x, y);
                    if (pixels[index] == 2 && IsPixelInPhase(mask, pixels, coord, x, y, 6))
                    {
                        pixels[index] = 0;
                    }
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var index = coord.Get(x, y);
                    /* if (pixels[index] == 1)
                     {
                         pixels[index] = 0;
                         pixels[index + 1] = 0;
                         pixels[index + 2] = 0;*/
                    if (pixels[index] == 0)
                    {
                        pixels[index] = 255;
                        pixels[index + 1] = 255;
                        pixels[index + 2] = 255;
                    }
                    else if (pixels[index] == 2)
                    {
                        pixels[index] = 255;
                    }
                    else if (pixels[index] == 3)
                    {
                        pixels[index + 2] = 255;
                    }
                    else if (pixels[index] == 4)
                    {
                        pixels[index + 1] = 255;
                    }
                    else
                    {
                        pixels[index] = 0;
                        pixels[index + 1] = 0;
                        pixels[index + 2] = 0;
                    }
                }
            }

            WriteableBitmap tmpBitmap = new WriteableBitmap(input);
            tmpBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, coord.Stride, 0);
            return tmpBitmap;
        }

        private bool IsPixelInPhase(int[,] mask, byte[] pixels, Coord coord, int x, int y, int phase)
        {
            var score = CalculateScore(pixels, coord, x, y, mask);
            //checking if in contact with 0 at all
            switch (phase)
            {
                case 0:
                    if (Tables.A0.Where(m => m == score).Count() > 0)
                        return true;
                    break;
                case 1:
                    if (Tables.A1.Where(m => m == score).Count() > 0)
                        return true;
                    break;
                case 2:
                    if (Tables.A2.Where(m => m == score).Count() > 0)
                        return true;
                    break;
                case 3:
                    if (Tables.A3.Where(m => m == score).Count() > 0)
                        return true;
                    break;
                case 4:
                    if (Tables.A4.Where(m => m == score).Count() > 0)
                        return true;
                    break;
                case 5:
                    if (Tables.A5.Where(m => m == score).Count() > 0)
                        return true;
                    break;
                case 6:
                    if (Tables.A1pix.Where(m => m == score).Count() > 0)
                        return true;
                    break;
                

            }           
             return false;
        }

        private WriteableBitmap KMM(WriteableBitmap input)
        {
            var height = input.PixelHeight;
            var width = input.PixelWidth;
            var coord = new Coord(height, width);
            var pixels = new byte[coord.Size];
            input.CopyPixels(pixels, coord.Stride, 0);

            int removedPixelsCounter = 0;
            //1. Black pixels = 1, white pixels = 0
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var index = coord.Get(x, y);
                    if (pixels[index] == 0)
                        pixels[index] = 1;
                    else
                    {
                        pixels[index] = 0;
                    }

                }
            }
            do
            {
                removedPixelsCounter = 0;
                //2 AND 3. 1's in contact with 0's are changed into 2. 1's in contact with 0's only by one corner of mask are changed into 3's
                var mask = GetMask();
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        var index = coord.Get(x, y);
                        if (pixels[index] != 0)
                        {
                            var pixelValue = Get23StagePixelValue(mask, pixels, coord, x, y);
                            pixels[index] = (byte)pixelValue;
                        }
                    }
                }

                ///4. Find 4's
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        var index = coord.Get(x, y);
                        if (pixels[index] != 0 && DeletePixel(mask, pixels, coord, x, y))
                        {
                            pixels[index] = 4;
                        }
                    }
                }
                //4. Remove 4's
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        var index = coord.Get(x, y);
                        if (pixels[index] == 4)
                        {
                            removedPixelsCounter++;
                            pixels[index] = 0;
                        }
                    }
                }
                //5. Determine whether 2's and 3's are connectors
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        var index = coord.Get(x, y);
                        if (pixels[index] == 2)
                        {
                            if (IsConnector(pixels, coord, x, y, mask))
                            {
                                pixels[index] = 0;
                                removedPixelsCounter++;
                            }
                            else
                            {
                                pixels[index] = 1;
                            }
                        }
                    }
                }
                //5. Determine whether 3's are connectors
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        var index = coord.Get(x, y);
                        if (pixels[index] == 3)
                        {
                            if (IsConnector(pixels, coord, x, y, mask))
                            {
                                pixels[index] = 0;
                                removedPixelsCounter++;
                            }
                            else
                            {
                                pixels[index] = 1;
                            }
                        }
                    }
                }
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        var index = coord.Get(x, y);
                        if (pixels[index] != 0)
                            pixels[index] = 1;
                    }
                }
                Console.WriteLine("Removed pixels: " + removedPixelsCounter);
            } while (removedPixelsCounter != 0);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var index = coord.Get(x, y);
                    /* if (pixels[index] == 1)
                     {
                         pixels[index] = 0;
                         pixels[index + 1] = 0;
                         pixels[index + 2] = 0;*/
                    if (pixels[index] == 0)
                    {
                        pixels[index] = 255;
                        pixels[index + 1] = 255;
                        pixels[index + 2] = 255;
                    }
                    else if (pixels[index] == 2)
                    {
                        pixels[index] = 255;
                    }
                    else if (pixels[index] == 3)
                    {
                        pixels[index + 2] = 255;
                    }
                    else if (pixels[index] == 4)
                    {
                        pixels[index + 1] = 255;
                    }
                    else
                    {
                        pixels[index] = 0;
                        pixels[index + 1] = 0;
                        pixels[index + 2] = 0;
                    }
                }
            }

            WriteableBitmap tmpBitmap = new WriteableBitmap(input);
            tmpBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, coord.Stride, 0);
            return tmpBitmap;
        }

        private bool IsConnector(byte[] pixels, Coord coord, int x, int y, int[,] mask)
        {
            var score = CalculateScore(pixels, coord, x, y, mask);
            if (Tables.deletionTable.Where(m => m == score).Count() > 0)
                return true;
            else
                return false;

        }

        private int Get23StagePixelValue(int[,] mask, byte[] pixels, Coord coord, int x, int y)
        {
            var score = CalculateScore(pixels, coord, x, y, mask);
            //checking if in contact with 0 at all
            if (score == 255)
                return 1;
            //checking for corners
            else if ((pixels[coord.Get(x, y - 1)] >= 1 && pixels[coord.Get(x + 1, y)] >= 1 && pixels[coord.Get(x, y + 1)] >= 1 && pixels[coord.Get(x - 1, y)] >= 1)
                && (pixels[coord.Get(x - 1, y - 1)] == 0 || pixels[coord.Get(x + 1, y - 1)] == 0 || pixels[coord.Get(x - 1, y + 1)] == 0 || pixels[coord.Get(x + 1, y + 1)] == 0))
                return 3;
            else
                return 2;

        }
        private bool DeletePixel(int[,] mask, byte[] pixels, Coord coord, int x, int y)
        {
            var score = CalculateScore(pixels, coord, x, y, mask);

            if (Tables.A234.Where(me => me == score).Count() > 0)
                return true;

            return false;
        }

        private int CalculateScore(byte[] pixels, Coord coord, int x, int y, int[,] mask)
        {
            var score = 0;
            for (int xm = x - 1, mx = 0; xm <= x + 1; xm++, mx++)
            {
                for (int ym = y - 1, my = 0; ym <= y + 1; ym++, my++)
                {
                    var val = pixels[coord.Get(xm, ym)] != 0 ? 1 : 0;
                    score += val * mask[mx, my];
                }
            }
            return score;
        }

        private int[,] GetMask()
        {
            var mask = new int[3, 3]
            {
                { 128, 1, 2 },
                { 64, 0, 4 },
                { 32, 16, 8}
            };
            return mask;
        }

        private WriteableBitmap ComputeThresholdImage(WriteableBitmap inputBitmap, int threshold)
        {
            int stride = inputBitmap.PixelWidth * 4;
            int size = inputBitmap.PixelHeight * stride;
            byte[] pixels = new byte[size];
            inputBitmap.CopyPixels(pixels, stride, 0);
            // if (sliderValue > prevSliderValue)
            // {
            for (int y = 0; y < inputBitmap.PixelHeight; y++)
            {
                for (int x = 0; x < inputBitmap.PixelWidth; x++)
                {
                    int index = y * stride + 4 * x;
                    //red
                    if (pixels[index] > threshold)
                    {
                        pixels[index] = 255;
                        pixels[index + 1] = 255;
                        pixels[index + 2] = 255;
                    }
                    else
                    {
                        pixels[index] = 0;
                        pixels[index + 1] = 0;
                        pixels[index + 2] = 0;
                    }
                    //alpha
                    byte alpha = pixels[index + 3];
                }
            }
            inputBitmap.WritePixels(new Int32Rect(0, 0, inputBitmap.PixelWidth, inputBitmap.PixelHeight), pixels, stride, 0);
            return inputBitmap;
        }

        private int CalculateThreshold(WriteableBitmap input, int offset)
        {
            var height = input.PixelHeight;
            var width = input.PixelWidth;
            var coord = new Coord(height, width);
            var pixels = new byte[coord.Size];
            input.CopyPixels(pixels, coord.Stride, 0);
            var sum = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    sum += pixels[coord.Get(x, y)];
                }
            }
            var threshold = (double)sum / (double)(width * height);
            return (int)threshold + offset;
        }

        private WriteableBitmap ErodeGrayscaleImage(WriteableBitmap input, int blackLevel = 10, byte difference = 40)
        {
            var height = input.PixelHeight;
            var width = input.PixelWidth;
            var coord = new Coord(height, width);
            var pixels = new byte[coord.Size];
            var newPixels = new byte[coord.Size];
            input.CopyPixels(pixels, coord.Stride, 0);
            pixels.CopyTo(newPixels, 0);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (x + 1 > width || x - 1 < 0 || y + 1 > height || y - 1 < 0)
                    {
                        var index = coord.Get(x, y);
                        if (newPixels[index] + difference < 255)
                        {
                            newPixels[index] += difference;
                            newPixels[index + 1] += difference;
                            newPixels[index + 2] += difference;
                        }
                        else
                        {
                            newPixels[index] = 255;
                            newPixels[index + 1] = 255;
                            newPixels[index + 2] = 255;
                        }
                    }
                    else if (coord.Get(x + 1, y + 1) < coord.Size && pixels[coord.Get(x, y)] < blackLevel && pixels[coord.Get(x, y - 1)] < blackLevel && pixels[coord.Get(x, y + 1)] < blackLevel && pixels[coord.Get(x + 1, y)] < blackLevel && pixels[coord.Get(x - 1, y)] < blackLevel)
                    {
                        var index = coord.Get(x, y);
                        if (newPixels[index] - difference > 0)
                        {
                            newPixels[index] -= difference;
                            newPixels[index + 1] -= difference;
                            newPixels[index + 2] -= difference;
                        }
                        else
                        {
                            newPixels[index] = 0;
                            newPixels[index + 1] = 0;
                            newPixels[index + 2] = 0;
                        }
                    }
                    else
                    {
                        var index = coord.Get(x, y);
                        if (newPixels[index] + difference < 255)
                        {
                            newPixels[index] += difference;
                            newPixels[index + 1] += difference;
                            newPixels[index + 2] += difference;
                        }
                        else
                        {
                            newPixels[index] = 255;
                            newPixels[index + 1] = 255;
                            newPixels[index + 2] = 255;
                        }
                    }
                }
            }
            WriteableBitmap tmpBitmap = new WriteableBitmap(input);
            tmpBitmap.WritePixels(new Int32Rect(0, 0, width, height), newPixels, coord.Stride, 0);
            return tmpBitmap;
        }

        private WriteableBitmap ToGrayScale(WriteableBitmap inputBitamp)
        {
            int width = inputBitamp.PixelWidth;
            int height = inputBitamp.PixelHeight;
            int stride = inputBitamp.PixelWidth * 4;
            int size = inputBitamp.PixelHeight * stride;
            byte[] pixels = new byte[size];

            inputBitamp.CopyPixels(pixels, stride, 0);
            for (int y = 0; y < inputBitamp.PixelHeight; y++)
            {
                for (int x = 0; x < inputBitamp.PixelWidth; x++)
                {
                    int index = y * stride + 4 * x;
                    byte red = pixels[index];
                    byte green = pixels[index + 1];
                    byte blue = pixels[index + 2];
                    int avg = (byte)((double)(red + green + blue) / 3.0);
                    pixels[index] = Convert.ToByte(avg);
                    pixels[index + 1] = Convert.ToByte(avg);
                    pixels[index + 2] = Convert.ToByte(avg);
                }
            }
            inputBitamp.WritePixels(new Int32Rect(0, 0, inputBitamp.PixelWidth, inputBitamp.PixelHeight), pixels, stride, 0);
            WriteableBitmap tmpBitmap = new WriteableBitmap(inputBitamp);
            tmpBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return tmpBitmap;
        }

        public static WriteableBitmap ConvolutionFunction(WriteableBitmap inputBitmap, int[][] filter, int valueD = 0, int offset = 0, int[] pivot = null)
        {
            int stride = inputBitmap.PixelWidth * 4;
            int size = inputBitmap.PixelHeight * stride;
            byte[] pixels = new byte[size];
            byte[] pixels2 = new byte[size];
            inputBitmap.CopyPixels(pixels, stride, 0);
            int filterWidth = filter.GetLength(0);
            int filterHeight = filter[0].GetLength(0);
            int pivotX = 0;
            int pivotY = 0;
            if (pivot == null)
            {
                pivotX = ((filterWidth - 1) / 2);
                pivotY = ((filterHeight - 1) / 2);
            }
            else
            {
                pivotX = pivot[0];
                pivotY = pivot[1];
            }
            for (int y = 0; y < inputBitmap.PixelHeight; y++)
            {
                for (int x = 0; x < inputBitmap.PixelWidth; x++)
                {

                    int sumRed = 0;
                    int sumGreen = 0;
                    int sumBlue = 0;
                    //weight of vector
                    int sumD = 0;
                    int index = (y) * stride + 4 * x;
                    for (int i = 0; i < filterWidth; i++)
                    {
                        for (int j = 0; j < filterHeight; j++)
                        {
                            int tmpIndex = (y + j - pivotY) * stride + 4 * (x + i - pivotX);
                            if (!((y + j - pivotY) < 0 || (y + j - pivotY) > inputBitmap.PixelHeight || (x + i - pivotX) < 0 || (x + i - pivotX) > inputBitmap.PixelWidth || tmpIndex >= pixels.Length))
                            {
                                sumRed += filter[i][j] * pixels[tmpIndex];
                                sumGreen += filter[i][j] * pixels[tmpIndex + 1];
                                sumBlue += filter[i][j] * pixels[tmpIndex + 2];
                                sumD += filter[i][j];
                            }
                        }
                    }
                    if (sumD == 0)
                    {
                        sumD = 1;
                    }
                    if (valueD != 0)
                    {
                        sumD = valueD;
                    }
                    sumRed = (sumRed / sumD) + offset;
                    sumGreen = (sumGreen / sumD) + offset;
                    sumBlue = (sumBlue / sumD) + offset;
                    index = y * stride + 4 * x;
                    //red
                    byte red = pixels[index];
                    if (sumRed > 255)
                    {
                        pixels2[index] = Convert.ToByte(255);
                    }
                    else if (sumRed < 0)
                    {
                        pixels2[index] = Convert.ToByte(0);
                    }
                    else
                        pixels2[index] = Convert.ToByte(sumRed);
                    //green
                    byte green = pixels[index + 1];
                    if (sumGreen > 255)
                    {
                        pixels2[index + 1] = Convert.ToByte(255);
                    }
                    else if (sumGreen < 0)
                    {
                        pixels2[index + 1] = Convert.ToByte(0);
                    }
                    else
                        pixels2[index + 1] = Convert.ToByte(sumGreen);
                    //blue
                    byte blue = pixels[index + 2];
                    if (sumBlue > 255)
                    {
                        pixels2[index + 2] = Convert.ToByte(255);
                    }
                    else if (sumBlue < 0)
                    {
                        pixels2[index + 2] = Convert.ToByte(0);
                    }
                    else
                        pixels2[index + 2] = Convert.ToByte(sumBlue);
                    //alpha
                    byte alpha = pixels[index + 3];
                    //tmp = 255 - alpha;
                    //pixels[index] = Convert.ToByte(tmp);
                }
            }
            inputBitmap.WritePixels(new Int32Rect(0, 0, inputBitmap.PixelWidth, inputBitmap.PixelHeight), pixels2, stride, 0);
            return inputBitmap;
        }

        public static int[][] CreateGaussianSmothingFilter(int gaussCoefficient)
        {
            int[][] gaussianSmothingFilter = new int[3][];
            for (int i = 0; i < 3; i++)
            {
                gaussianSmothingFilter[i] = new int[3];
                for (int j = 0; j < 3; j++)
                {
                    gaussianSmothingFilter[i][j] = 1;
                }
            }
            gaussianSmothingFilter[0][1] = gaussCoefficient;
            gaussianSmothingFilter[1][0] = gaussCoefficient;
            gaussianSmothingFilter[1][1] = gaussCoefficient * gaussCoefficient;
            gaussianSmothingFilter[1][2] = gaussCoefficient;
            gaussianSmothingFilter[2][1] = gaussCoefficient;
            return gaussianSmothingFilter;
        }
    }
}
