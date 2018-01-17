using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using IES;

namespace IES
{
    public partial class Form1 : Form
    {
        Bitmap bmpPicture;
        System.Drawing.Imaging.ImageAttributes iaPicture;
        System.Drawing.Imaging.ColorMatrix cmPicture;
        Graphics gfxPicture;
        Rectangle rctPicture;


        ImageHandler imagehandler = new ImageHandler();

        // cropping view stuff
        Rectangle CropRect;
        Rectangle rcLT, rcRT, rcLB, rcRB;
        Rectangle rcOld, rcNew;
        Rectangle rcOriginal;
        Rectangle rcBegin;
        SolidBrush BrushRect;
        HatchBrush BrushRectSmall;
        Color BrushColor;

        int AlphaBlend;
        int nSize;
        int nWd;
        int nHt;
        int nResizeRT;
        int nResizeBL;
        int nResizeLT;
        int nResizeRB;
        int nThatsIt;
        int nCropRect;
        int CropWidth;

        int imageWidth;
        int imageHeight;
        int HeightOffset;



        double CropAspectRatio;

        double ZoomedRatio;

        Point ptOld;
        Point ptNew;

        private int b_track;
        private int n_track;


        string imageStats;
        string filename;


        //Resize Stuff
        private Size OriginalImageSize;







        //Bitmap Image Property

        private Bitmap originalBitmap = null;
        private Bitmap previewBitmap = null;
        private Bitmap resultBitmap = null;

        List<double> ratios;
        public Form1()
        {
            InitializeComponent(); this.SetStyle(
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer, true);

            // build list of crop ratios
            ratios = new List<double>();
            comboBoxAspectRatio.Items.Add("4:3  (1.3)  Normal Display");
            ratios.Add(4.0 / 3.0);
            comboBoxAspectRatio.Items.Add("16:9 (1.78) High Definition");
            ratios.Add(16.0 / 9.0);
            comboBoxAspectRatio.Items.Add("3:2 (1.5) Digital Camera");
            ratios.Add(3.0 / 2.0);
            comboBoxAspectRatio.Items.Add("1:1 (1.0) Square");
            ratios.Add(1.0);
            comboBoxAspectRatio.SelectedIndex = 0;
            // build list of common sizes
            comboBoxCropSize.Items.Add("320");
            comboBoxCropSize.Items.Add("640");
            comboBoxCropSize.Items.Add("800");
            comboBoxCropSize.Items.Add("1024");
            comboBoxCropSize.SelectedIndex = 0;
            CropWidth = Convert.ToInt16(comboBoxCropSize.Text);
            comboBoxFilter.Items.Add(ColorSwapFilter.ColorSwapType.ShiftLeft);
            comboBoxFilter.Items.Add(ColorSwapFilter.ColorSwapType.ShiftRight);
            comboBoxFilter.Items.Add(ColorSwapFilter.ColorSwapType.SwapBlueAndGreen);
            comboBoxFilter.Items.Add(ColorSwapFilter.ColorSwapType.SwapBlueAndRed);
            comboBoxFilter.Items.Add(ColorSwapFilter.ColorSwapType.SwapRedAndGreen);

            comboBoxFilter.SelectedIndex = 0;

            // Insert Text Stuff

            // Load Fonts.
            foreach (FontFamily ff in FontFamily.Families)
            {
                comboBoxFont.Items.Add(ff.Name);
            }
            // Load Font Size.
            for (int i = 5; i <= 75; i += 5)
            {
                comboBoxFontSize.Items.Add(i.ToString() + "pt");
            }
            // Load Font Styles.
            comboBoxFontStyle.Items.Add("Bold");
            comboBoxFontStyle.Items.Add("Italic");
            comboBoxFontStyle.Items.Add("Regular");
            comboBoxFontStyle.Items.Add("Strikeout");
            comboBoxFontStyle.Items.Add("Underline");

            // Load Color 

            Type type = typeof(System.Drawing.Color);
            System.Reflection.PropertyInfo[] propertyInfo = type.GetProperties();
            for (int i = 0; i < propertyInfo.Length; i++)
            {
                if (propertyInfo[i].Name != "Transparent"
                    && propertyInfo[i].Name != "R"
                    && propertyInfo[i].Name != "G"
                    && propertyInfo[i].Name != "B"
                    && propertyInfo[i].Name != "A"
                    && propertyInfo[i].Name != "IsKnownColor"
                    && propertyInfo[i].Name != "IsEmpty"
                    && propertyInfo[i].Name != "IsNamedColor"
                    && propertyInfo[i].Name != "IsSystemColor"
                    && propertyInfo[i].Name != "Name")
                {
                    comboBoxColor1.Items.Add(propertyInfo[i].Name);
                    comboBoxColor2.Items.Add(propertyInfo[i].Name);
                }
            }

            // offset to make width & height proportional to image
            HeightOffset = statusStrip1.Height + menuStrip1.Height + toolStrip1.Height + panelLeft.Width +
               panelTop.Height + panelBottom.Height + panelRight.Width + SystemInformation.CaptionHeight + (SystemInformation.BorderSize.Height * 2);


        }

        void InitializeCropRectangle()
        {
            AlphaBlend = 48;

            nSize = 8;
            nWd = CropWidth = Convert.ToInt16(comboBoxCropSize.Text);
            nHt = 1;

            nThatsIt = 0;
            nResizeRT = 0;
            nResizeBL = 0;
            nResizeLT = 0;
            nResizeRB = 0;

            CropAspectRatio = ratios[comboBoxAspectRatio.SelectedIndex];

            BrushColor = Color.White;
            BrushRect = new SolidBrush(Color.FromArgb(AlphaBlend, BrushColor.R, BrushColor.G, BrushColor.B));

            BrushColor = Color.Yellow;
            BrushRectSmall = new HatchBrush(HatchStyle.Percent50, Color.FromArgb(192, BrushColor.R, BrushColor.G, BrushColor.B));

            ptOld = new Point(0, 0);
            rcBegin = new Rectangle();
            rcOriginal = new Rectangle(0, 0, 0, 0);
            rcLT = new Rectangle(0, 0, nSize, nSize);
            rcRT = new Rectangle(0, 0, nSize, nSize);
            rcLB = new Rectangle(0, 0, nSize, nSize);
            rcRB = new Rectangle(0, 0, nSize, nSize);
            rcOld = CropRect = new Rectangle(0, 0, nWd, nHt);


        }
        public void AdjustResizeRects()
        {
            rcLT.X = CropRect.Left;
            rcLT.Y = CropRect.Top;

            rcRT.X = CropRect.Right - rcRT.Width;
            rcRT.Y = CropRect.Top;

            rcLB.X = CropRect.Left;
            rcLB.Y = CropRect.Bottom - rcLB.Height;

            rcRB.X = CropRect.Right - rcRB.Width;
            rcRB.Y = CropRect.Bottom - rcRB.Height;
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void aboutUsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ContactUsForm contform = new ContactUsForm();
            contform.Hide();

            AboutUsForm abtfrm = new AboutUsForm();
            abtfrm.Show();
        }

        private void contactToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutUsForm abtfrm = new AboutUsForm();
            abtfrm.Hide();

            ContactUsForm contform = new ContactUsForm();
            contform.Show();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
                    }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void buttonRotate90_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.AppStarting;
            pictureBox1.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);

            pictureBox1.Refresh();
            Cursor = Cursors.Default;

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void toolOpen_Click(object sender, EventArgs e)
        {
            if (openImage.ShowDialog() == DialogResult.OK)
            {
                filename = openImage.FileName;
                LoadImage(filename);
                imagehandler.CurrentBitmap = (Bitmap)pictureBox1.Image;
                imagehandler.BitmapPath = openImage.FileName;
                StreamReader streamReader = new StreamReader(openImage.FileName);
                originalBitmap = (Bitmap)pictureBox1.Image;
                imagehandler.CurrentBitmap = originalBitmap;
                streamReader.Close();
                pictureBox2.Image = imagehandler.CurrentBitmap;
                //pictureBox2.BackgroundImage = originalBitmap;
                previewBitmap = originalBitmap.CopyToSquareCanvas(pictureBox1.Width);
                previewBitmap = (Bitmap)pictureBox1.Image;
                pictureBox1.Image = previewBitmap;
                pictureBox1.Image = imagehandler.CurrentBitmap;
                pictureBox2.Image = imagehandler.CurrentBitmap;
                imageInfoToolStripMenuItem.Enabled = true;
                saveToolStripMenuItem.Enabled = true;
                toolSave.Enabled = true;
            }


        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openImage.ShowDialog() == DialogResult.OK)
            {
                filename = openImage.FileName;
                LoadImage(filename);
                imagehandler.CurrentBitmap = (Bitmap)pictureBox1.Image;
                imagehandler.BitmapPath = openImage.FileName;
                StreamReader streamReader = new StreamReader(openImage.FileName);
                originalBitmap = (Bitmap)pictureBox1.Image;
                imagehandler.CurrentBitmap = originalBitmap;
                streamReader.Close();
                pictureBox2.Image = imagehandler.CurrentBitmap;
                //pictureBox2.BackgroundImage = originalBitmap;
                previewBitmap = originalBitmap.CopyToSquareCanvas(pictureBox1.Width);
                previewBitmap = (Bitmap)pictureBox1.Image;
                pictureBox1.Image = previewBitmap;
                pictureBox1.Image = imagehandler.CurrentBitmap;
                pictureBox2.Image = imagehandler.CurrentBitmap;
                imageInfoToolStripMenuItem.Enabled = true;
                saveToolStripMenuItem.Enabled = true;
                toolSave.Enabled = true;
            }
        }

        private void buttonRotate180_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.AppStarting;
            pictureBox1.Image.RotateFlip(RotateFlipType.Rotate180FlipNone);
            pictureBox1.Refresh();
            Cursor = Cursors.Default;
        }

        private void buttonRotate270_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.AppStarting;
            pictureBox1.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
            pictureBox1.Refresh();
            Cursor = Cursors.Default;
        }

        private void buttonFlipX_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.AppStarting;
            pictureBox1.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);
            pictureBox1.Refresh();
            Cursor = Cursors.Default;
        }

        private void buttonFlipY_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.AppStarting;
            pictureBox1.Image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            pictureBox1.Refresh();
            Cursor = Cursors.Default;
        }



        private void ApplyContrast(bool preview)
        {
            if (previewBitmap == null)
            {
                return;
            }

            if (preview == true)
            {
                pictureBox1.Image = previewBitmap.Contrast(trackBarContrast.Value);
            }
            else
            {
                resultBitmap = originalBitmap.Contrast(trackBarContrast.Value);
            }
        }
        private void ThresholdValueChangedEventHandler(object sender, EventArgs e)
        {
            labelContrast.Text = trackBarContrast.Value.ToString();

            ApplyContrast(true);
        }

        public void ResetBitmap()
        {
            imagehandler.CurrentBitmap = (Bitmap)pictureBox1.Image;
            if (imagehandler.CurrentBitmap != null && imagehandler.BitmapBeforeProcessing != null)
            {
                Bitmap temp = (Bitmap)imagehandler.CurrentBitmap.Clone();
                imagehandler.CurrentBitmap = (Bitmap)imagehandler.BitmapBeforeProcessing.Clone();
                imagehandler.BitmapBeforeProcessing = (Bitmap)temp.Clone();
            }
        }

        public void SaveBitmap(string saveFilePath)
        {
            imagehandler.BitmapPath = saveFilePath;
            if (System.IO.File.Exists(saveFilePath))
            {
                System.IO.File.Delete(saveFilePath);
            }
            imagehandler.CurrentBitmap.Save(saveFilePath);
        }


           
        


        private void toolSave_Click(object sender, EventArgs e)
        {
            //ApplyBitmapColorBalance(false);
            //ApplyCartoon(false);
            //ApplyColorFilter();
            ApplyContrast(false);
            ApplyDistortion(false);
            ApplyTransformRotate(false);
            if (resultBitmap != null)
            {


                if (saveImage.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string fileExtension = Path.GetExtension(saveImage.FileName).ToUpper();
                    ImageFormat imgFormat = ImageFormat.Png;

                    if (fileExtension == "BMP")
                    {
                        imgFormat = ImageFormat.Bmp;
                    }
                    else if (fileExtension == "JPG")
                    {
                        imgFormat = ImageFormat.Jpeg;
                    }
                    SaveBitmap(saveImage.FileName);
                    StreamWriter streamWriter = new StreamWriter(saveImage.FileName, false);
                    resultBitmap.Save(streamWriter.BaseStream, imgFormat);
                    streamWriter.Flush();
                    streamWriter.Close();


                    resultBitmap = null;
                }
            }
            else
            {
                if (DialogResult.OK == saveImage.ShowDialog())
                {
                    SaveBitmap(saveImage.FileName);
                }
            }

        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //ApplyBitmapColorBalance(false);
            //ApplyCartoon(false);
            //ApplyColorFilter();
            ApplyContrast(false);
            ApplyDistortion(false);
            ApplyTransformRotate(false);
            if (resultBitmap != null)
            {


                if (saveImage.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string fileExtension = Path.GetExtension(saveImage.FileName).ToUpper();
                    ImageFormat imgFormat = ImageFormat.Png;

                    if (fileExtension == "BMP")
                    {
                        imgFormat = ImageFormat.Bmp;
                    }
                    else if (fileExtension == "JPG")
                    {
                        imgFormat = ImageFormat.Jpeg;
                    }
                    SaveBitmap(saveImage.FileName);
                    StreamWriter streamWriter = new StreamWriter(saveImage.FileName, false);
                    resultBitmap.Save(streamWriter.BaseStream, imgFormat);
                    streamWriter.Flush();
                    streamWriter.Close();


                    resultBitmap = null;
                }
            }
            else
            {
                if (DialogResult.OK == saveImage.ShowDialog())
                {
                    SaveBitmap(saveImage.FileName);
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetBitmap();

            this.Invalidate();
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadImage(filename);
        }

        private void clearImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = null;

        }

        private void imageInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImageInfo imginfo = new ImageInfo(imagehandler);
            imginfo.Show();
        }


        private void statusBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveToolStripMenuItem.Checked)
            {
                statusStrip1.Visible = true;
            }
            else
            {
                statusStrip1.Visible = false;
            }
        }

        private void buttonTransformRotation_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                cropcheckBox.Checked = false;
                checkBoxInsertImage.Checked = false;
                panelRight.Visible = true;
                splitContainer1.Visible = true;
                panelCrop.Visible = true;
                panelContrast.Visible = true;
                panelColorFilter.Visible = true;
                panelDistortion.Visible = true;
                panelColorEffect.Visible = true;
                panelInsertImage.Visible = true;
                panelTransformRotation.Visible = true;
                panelInsertText.Visible = false;


            }
        }

        private void buttonOilPaint_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                cropcheckBox.Checked = false;
                panelRight.Visible = true;
                splitContainer1.Visible = true;
                panelCrop.Visible = true;
                panelContrast.Visible = true;
                panelColorFilter.Visible = true;
                panelDistortion.Visible = false;
                panelColorEffect.Visible = false;
                panelInsertImage.Visible = false;
                panelInsertText.Visible = false;
                panelTransformRotation.Visible = false;

            }
        }

        private void cropcheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (cropcheckBox.Checked == true)
            {

                comboBoxAspectRatio.Enabled = true;
                comboBoxCropSize.Enabled = true;
                buttonSelectCrop.Enabled = true;
                buttonSaveCrop.Enabled = true;
                buttonSelectCrop_Click(null, null);

            }
            else
            {
                comboBoxAspectRatio.Enabled = false;
                comboBoxCropSize.Enabled = false;
                buttonSaveCrop.Enabled = false;
                buttonSaveCrop.Enabled = false;


            }
        }

        private void comboBoxAspectRatio_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateAspectRatio();
        }

        private void comboBoxCropSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            CropWidth = Convert.ToInt16(comboBoxCropSize.Text);

            CropRect.X = 0;
            CropRect.Y = 0;

            UpdateAspectRatio();
        }

        private void UpdateAspectRatio()
        {
            int ratioIndex = comboBoxAspectRatio.SelectedIndex;

            CropAspectRatio = ratios[ratioIndex];
            int CropHeight = (int)((CropWidth / CropAspectRatio));

            try
            {
                ZoomedRatio = pictureBox1.ClientRectangle.Width / (double)imageWidth;
            }
            catch
            {
                // imageWidth is not yet established (division by zero)
                // force a value
                ZoomedRatio = 1.0;
            }

            // scale crop rect to image scale
            CropRect.Width = (int)((double)CropWidth * ZoomedRatio);
            CropRect.Height = (int)((double)CropHeight * ZoomedRatio);

            // update crop box and refresh everything
            nThatsIt = 1;


        }

        private void buttonSelectCrop_Click(object sender, EventArgs e)
        {
            UpdateAspectRatio();

            CropRect.X = (pictureBox1.ClientRectangle.Width - CropRect.Width) / 2;
            CropRect.Y = (pictureBox1.ClientRectangle.Height - CropRect.Height) / 2;
            pictureBox1.Refresh();
        }

        private void buttonSaveCrop_Click(object sender, EventArgs e)
        {
            // output image size is based upon the visible crop rectangle and scaled to 
            // the ratio of actual image size to displayed image size
            originalBitmap = (Bitmap)pictureBox1.Image;
            Bitmap temp = (Bitmap)originalBitmap.Clone();
            Bitmap bmap = (Bitmap)temp.Clone();

            Rectangle ScaledCropRect = new Rectangle();
            ScaledCropRect.X = (int)(CropRect.X / ZoomedRatio);
            ScaledCropRect.Y = (int)(CropRect.Y / ZoomedRatio);
            ScaledCropRect.Width = (int)((double)(CropRect.Width) / ZoomedRatio);
            ScaledCropRect.Height = (int)((double)(CropRect.Height) / ZoomedRatio);
            try
            {
                bmap = (Bitmap)CropImage(pictureBox1.Image, ScaledCropRect);
                Bitmap cropimage = (Bitmap)bmap.Clone();

                originalBitmap = (Bitmap)cropimage.Clone();
                pictureBox1.Image = originalBitmap;
                originalBitmap = (Bitmap)pictureBox1.Image;
                //85% quality
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "btnOK_Click()");
            }
            if (bmap != null)
                bmap.Dispose();

            pictureBox1.Image = originalBitmap;
            pictureBox1.Refresh();
            buttonSelectCrop.Enabled = false;
            buttonSaveCrop.Enabled = false;

            pictureBox2.Image = originalBitmap;
            pictureBox2.Refresh();
        }
        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            Cursor = Cursors.Default;
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (nThatsIt == 0)
                return;

            nCropRect = 0;
            nResizeRB = 0;
            nResizeBL = 0;
            nResizeRT = 0;
            nResizeLT = 0;

            if (CropRect.Width <= 0 || CropRect.Height <= 0)
                CropRect = rcOriginal;

            if (CropRect.Right > ClientRectangle.Width)
                CropRect.Width = ClientRectangle.Width - CropRect.X;

            if (CropRect.Bottom > ClientRectangle.Height)
                CropRect.Height = ClientRectangle.Height - CropRect.Y;

            if (CropRect.X < 0)
                CropRect.X = 0;

            if (CropRect.Y < 0)
                CropRect.Y = 0;

            // need to add logic for portrait mode of crop box in this
            // area

            // now that the crop box position is established
            // force it to the proper aspect ratio
            // and scale it

            if (CropRect.Width > CropRect.Height)
            {
                CropRect.Height = (int)(CropRect.Width / CropAspectRatio);
            }
            else
            {
                CropRect.Width = (int)(CropRect.Height * CropAspectRatio);
            }

            AdjustResizeRects();
            pictureBox1.Refresh();

            base.OnMouseUp(e);

            nWd = rcNew.Width;
            nHt = rcNew.Height;
            rcBegin = rcNew;
            DisplayLocation();

        }
        private void DisplayLocation()
        {
            // assume not yet initialized
            if (pictureBox1.Image == null)
                return;

            String.Format("{0} |  Scale {1:0.00}% | Crop Area {2} x {3} | Crop X, Y {4}, {5}",
            imageStats,
            ZoomedRatio * 100.0,
            (int)((double)CropRect.Width / ZoomedRatio),
            (int)((double)CropRect.Height / ZoomedRatio),
            (int)((double)CropRect.X / ZoomedRatio),
            (int)((double)CropRect.Y / ZoomedRatio)
        );
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            Point pt = new Point(e.X, e.Y);
            rcOriginal = CropRect;
            rcBegin = CropRect;

            if (rcRB.Contains(pt))
            {
                rcOld = new Rectangle(CropRect.X, CropRect.Y, CropRect.Width, CropRect.Height);
                rcNew = rcOld;
                nResizeRB = 1;
            }
            else
                if (rcLB.Contains(pt))
                {
                    rcOld = new Rectangle(CropRect.X, CropRect.Y, CropRect.Width, CropRect.Height);
                    rcNew = rcOld;
                    nResizeBL = 1;
                }
                else
                    if (rcRT.Contains(pt))
                    {
                        rcOld = new Rectangle(CropRect.X, CropRect.Y, CropRect.Width, CropRect.Height);
                        rcNew = rcOld;
                        nResizeRT = 1;
                    }
                    else
                        if (rcLT.Contains(pt))
                        {
                            rcOld = new Rectangle(CropRect.X, CropRect.Y, CropRect.Width, CropRect.Height);
                            rcNew = rcOld;
                            nResizeLT = 1;
                        }
                        else
                            if (CropRect.Contains(pt))
                            {
                                nResizeBL = nResizeLT = nResizeRB = nResizeRT = 0;
                                nCropRect = 1;
                                ptNew = ptOld = pt;
                            }
            nThatsIt = 1;
            base.OnMouseDown(e);
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image == null)
                return;

            Point pt = new Point(e.X, e.Y);

            if (rcLT.Contains(pt))
                Cursor = Cursors.SizeNWSE;
            else
                if (rcRT.Contains(pt))
                    Cursor = Cursors.SizeNESW;
                else
                    if (rcLB.Contains(pt))
                        Cursor = Cursors.SizeNESW;
                    else
                        if (rcRB.Contains(pt))
                            Cursor = Cursors.SizeNWSE;
                        else
                            if (CropRect.Contains(pt))
                                Cursor = Cursors.SizeAll;
                            else
                                Cursor = Cursors.Default;


            if (e.Button == MouseButtons.Left)
            {
                if (nResizeRB == 1)
                {
                    rcNew.X = CropRect.X;
                    rcNew.Y = CropRect.Y;
                    rcNew.Width = pt.X - rcNew.Left;
                    rcNew.Height = pt.Y - rcNew.Top;

                    if (rcNew.X > rcNew.Right)
                    {
                        rcNew.Offset(-nWd, 0);
                        if (rcNew.X < 0)
                            rcNew.X = 0;
                    }
                    if (rcNew.Y > rcNew.Bottom)
                    {
                        rcNew.Offset(0, -nHt);
                        if (rcNew.Y < 0)
                            rcNew.Y = 0;
                    }

                    DrawDragRect(e);
                    rcOld = CropRect = rcNew;
                    Cursor = Cursors.SizeNWSE;
                }
                else
                    if (nResizeBL == 1)
                    {
                        rcNew.X = pt.X;
                        rcNew.Y = CropRect.Y;
                        rcNew.Width = CropRect.Right - pt.X;
                        rcNew.Height = pt.Y - rcNew.Top;

                        if (rcNew.X > rcNew.Right)
                        {
                            rcNew.Offset(nWd, 0);
                            if (rcNew.Right > ClientRectangle.Width)
                                rcNew.Width = ClientRectangle.Width - rcNew.X;
                        }
                        if (rcNew.Y > rcNew.Bottom)
                        {
                            rcNew.Offset(0, -nHt);
                            if (rcNew.Y < 0)
                                rcNew.Y = 0;
                        }

                        DrawDragRect(e);
                        rcOld = CropRect = rcNew;
                        Cursor = Cursors.SizeNESW;
                    }
                    else
                        if (nResizeRT == 1)
                        {
                            rcNew.X = CropRect.X;
                            rcNew.Y = pt.Y;
                            rcNew.Width = pt.X - rcNew.Left;
                            rcNew.Height = CropRect.Bottom - pt.Y;

                            if (rcNew.X > rcNew.Right)
                            {
                                rcNew.Offset(-nWd, 0);
                                if (rcNew.X < 0)
                                    rcNew.X = 0;
                            }
                            if (rcNew.Y > rcNew.Bottom)
                            {
                                rcNew.Offset(0, nHt);
                                if (rcNew.Bottom > ClientRectangle.Height)
                                    rcNew.Y = ClientRectangle.Height - rcNew.Height;
                            }

                            DrawDragRect(e);
                            rcOld = CropRect = rcNew;
                            Cursor = Cursors.SizeNESW;
                        }
                        else
                            if (nResizeLT == 1)
                            {
                                rcNew.X = pt.X;
                                rcNew.Y = pt.Y;
                                rcNew.Width = CropRect.Right - pt.X;
                                rcNew.Height = CropRect.Bottom - pt.Y;

                                if (rcNew.X > rcNew.Right)
                                {
                                    rcNew.Offset(nWd, 0);
                                    if (rcNew.Right > ClientRectangle.Width)
                                        rcNew.Width = ClientRectangle.Width - rcNew.X;
                                }
                                if (rcNew.Y > rcNew.Bottom)
                                {
                                    rcNew.Offset(0, nHt);
                                    if (rcNew.Bottom > ClientRectangle.Height)
                                        rcNew.Height = ClientRectangle.Height - rcNew.Y;
                                }

                                DrawDragRect(e);
                                rcOld = CropRect = rcNew;
                                Cursor = Cursors.SizeNWSE;
                            }
                            else
                                if (nCropRect == 1) //Moving the rectangle
                                {
                                    ptNew = pt;
                                    int dx = ptNew.X - ptOld.X;
                                    int dy = ptNew.Y - ptOld.Y;
                                    CropRect.Offset(dx, dy);
                                    rcNew = CropRect;
                                    DrawDragRect(e);
                                    ptOld = ptNew;
                                }

                AdjustResizeRects();
                DisplayLocation();
                pictureBox1.Update();
            }


        }
        private Bitmap CropImage(Image image, Rectangle ScaledCropRect)
        {
            throw new NotImplementedException();
        }
        private void DrawDragRect(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                AdjustResizeRects();
                pictureBox1.Invalidate();
            }
        }

        private void LoadImage(string file)
        {
            Cursor = Cursors.AppStarting;

            pictureBox1.Image = Image.FromFile(file);

            imageWidth = pictureBox1.Image.Width;
            imageHeight = pictureBox1.Image.Height;

            imageStats = String.Format("{0} | {1}x{2} | Aspect {3:0.0}",
                System.IO.Path.GetFileName(file), imageWidth, imageHeight,
                (double)((double)imageWidth / (double)imageHeight)
                );



            OriginalImageSize = new Size(imageWidth, imageHeight);

            buttonSelectCrop_Click(null, null);



            Cursor = Cursors.Default;
        }



        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                // display checkerboard
                bool xGrayBox = true;
                int backgroundX = 0;
                while (backgroundX < pictureBox1.Width)
                {
                    int backgroundY = 0;
                    bool yGrayBox = xGrayBox;
                    while (backgroundY < pictureBox1.Height)
                    {
                        int recWidth = (int)((backgroundX + 50 > pictureBox1.Width) ? pictureBox1.Width - backgroundX : 50);
                        int recHeight = (int)((backgroundY + 50 > pictureBox1.Height) ? pictureBox1.Height - backgroundY : 50);
                        e.Graphics.FillRectangle(((Brush)(yGrayBox ? Brushes.LightGray : Brushes.Gainsboro)), backgroundX, backgroundY, recWidth + 2, recHeight + 2);
                        backgroundY += 50;
                        yGrayBox = !yGrayBox;
                    }
                    backgroundX += 50;
                    xGrayBox = !xGrayBox;
                }
            }
            else if (cropcheckBox.Checked)
            {

                // main crop box 
                e.Graphics.FillRectangle((BrushRect), CropRect);

                // corner drag boxes
                e.Graphics.FillRectangle((BrushRectSmall), rcLT);
                e.Graphics.FillRectangle((BrushRectSmall), rcRT);
                e.Graphics.FillRectangle((BrushRectSmall), rcLB);
                e.Graphics.FillRectangle((BrushRectSmall), rcRB);

                AdjustResizeRects();
            }

        }



        private void SetSize()
        {
            panelMiddle.Height = pictureBox1.Image.Height;
            panelMiddle.Width = pictureBox1.Image.Width;
        }

        public static bool Brighten(Bitmap b, int nBrightness)
        {
            // GDI+ return format is BGR, NOT RGB. 
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int stride = bmData.Stride;
            System.IntPtr Scan0 = bmData.Scan0;
            unsafe
            {
                int nVal;
                byte* p = (byte*)(void*)Scan0;
                int nOffset = stride - b.Width * 3;
                int nWidth = b.Width * 3;
                for (int y = 0; y < b.Height; ++y)
                {
                    for (int x = 0; x < nWidth; ++x)
                    {
                        nVal = (int)(p[0] + nBrightness);
                        if (nVal < 0) nVal = 0;
                        if (nVal > 255) nVal = 255;
                        p[0] = (byte)nVal;
                        ++p;
                    }
                    p += nOffset;
                }
            }
            b.UnlockBits(bmData);
            return true;
        }



        private void trackBarBrightness_Scroll(object sender, EventArgs e)
        {
            b_track = trackBarBrightness.Value;
            labelBrightness.Text = b_track.ToString();

            Bitmap temp = new Bitmap(originalBitmap);
            Brighten(temp, b_track);
            pictureBox1.Image = temp;

        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            trackBarBrightness.SetRange(0, 100);
            b_track = 0;
            trackBarBrightness.Value = b_track;
            labelBrightness.Text = b_track.ToString();
            trackBarContrast.SetRange(0, 100);
            b_track = 0;
            trackBarContrast.Value = b_track;
            labelContrast.Text = b_track.ToString();
            trackBarNoise.SetRange(0, 100);
            b_track = 0;
            trackBarNoise.Value = b_track;
            labelNoise.Text = b_track.ToString();

        }

        private void trackBarNoise_Scroll(object sender, EventArgs e)
        {
            n_track = trackBarNoise.Value;
            labelNoise.Text = n_track.ToString();
            Bitmap temp = new Bitmap(originalBitmap);
            Brighten(temp, -n_track);
            pictureBox1.Image = temp;

        }

        private void panelContrast_Paint(object sender, PaintEventArgs e)
        {

        }





        public Image originalbitmap { get; set; }

        private void panelColorFilter_Paint(object sender, PaintEventArgs e)
        {

        }
        private void ApplyColorFilter()
        {
            if (pictureBox1.Image != null)
            {

                ColorSwapFilter swapFilter = new ColorSwapFilter();
                swapFilter.SwapType = (ColorSwapFilter.ColorSwapType)comboBoxFilter.SelectedItem;
                swapFilter.InvertColorsWhenSwapping = checkBoxInvertColor.Checked;
                swapFilter.SwapHalfColorValues = checkBoxHalfColor.Checked;

                pictureBox1.Image = ((Bitmap)(pictureBox2.Image)).SwapColorsCopy(swapFilter);

            }
            pictureBox1.Refresh();
        }

        private void comboBoxFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyColorFilter();
        }

        private void checkBoxInvertColor_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBoxHalfColor_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void panelOilPaint_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }
        

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        

        private void label8_Click_1(object sender, EventArgs e)
        {

        }

        private void trackBarRed_Scroll(object sender, EventArgs e)
        {
          
            

        }

        private void panelBitmapColor_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void panelDistortion_Paint(object sender, PaintEventArgs e)
        {

        }

        private void ApplyDistortion(bool preview)
        {
            Bitmap selectedSource = null;
            Bitmap bitmapResult = null;

            if (preview == true)
            {
                selectedSource = previewBitmap;
            }
            else
            {
                selectedSource = originalBitmap;
            }

            if (selectedSource != null)
            {
                bitmapResult = selectedSource.DistortionBlurFilter((int)numericDistortion.Value);
            }

            if (bitmapResult != null)
            {
                if (preview == true)
                {
                    pictureBox1.Image = bitmapResult;
                }
                else
                {
                    resultBitmap = bitmapResult;
                }
            }
        }

        private void numericDistortion_ValueChanged(object sender, EventArgs e)
        {
            ApplyDistortion(true);
        }

        private void panelColorEffect_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void PreparePicture()
        {
            // If there's a picture
            if (pictureBox1.Image != null)
            {
                // Create new Bitmap object with the size of the picture
                bmpPicture = new Bitmap(pictureBox1.Image.Width, pictureBox1.Image.Height);

                // Image attributes for setting the attributes of the picture
                iaPicture = new System.Drawing.Imaging.ImageAttributes();
            }
        }

        private void FinalizePicture()
        {
            // Set the new color matrix
            iaPicture.SetColorMatrix(cmPicture);

            // Set the Graphics object from the bitmap
            gfxPicture = Graphics.FromImage(bmpPicture);

            // New rectangle for the picture, same size as the original picture
            rctPicture = new Rectangle(0, 0, pictureBox1.Image.Width, pictureBox1.Image.Height);

            // Draw the new image
            gfxPicture.DrawImage(pictureBox1.Image, rctPicture, 0, 0, pictureBox1.Image.Width, pictureBox1.Image.Height, GraphicsUnit.Pixel, iaPicture);

            // Set the PictureBox to the new bitmap
            pictureBox1.Image = bmpPicture;
            imagehandler.CurrentBitmap = bmpPicture;
        }

        private void buttonInvert_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.AppStarting;

            PreparePicture();
            cmPicture = new System.Drawing.Imaging.ColorMatrix(new float[][]
            {
                new float[] {-1, 0, 0, 0, 0},
                new float[] {0, -1, 0, 0, 0},
                new float[] {0, 0, -1, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {1, 1, 1, 0, 1}
            });
            FinalizePicture();

            Cursor = Cursors.Default;
        }

        private void buttonGrayscale_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.AppStarting;

            PreparePicture();
            cmPicture = new System.Drawing.Imaging.ColorMatrix(new float[][]
            {
                new float[] {0.30f, 0.30f, 0.30f, 0, 0},
                new float[] {0.59f, 0.59f, 0.59f, 0, 0},
                new float[] {0.11f, 0.11f, 0.11f, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1}
            });

            FinalizePicture();

            Cursor = Cursors.Default;

        }

        private void buttonSepia_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = pictureBox2.Image.CopyAsSepiaTone();
        }

        private void buttonTransparcy_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = pictureBox2.Image.CopyWithTransparency();
        }

        private void panel1_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void label17_Click(object sender, EventArgs e)
        {

        }

        private void checkBoxInsertImage_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxInsertImage.Checked == true)
            {
                buttonSelectCrop_Click(null, null);
            }
        }

        private void buttonaddImage_Click(object sender, EventArgs e)
        {
            if (openImage2.ShowDialog() == DialogResult.OK)
            {
                filename = openImage2.FileName;
                textBoxImage.Text = openImage2.FileName;
                checkBoxInsertImage.Checked = true;
            }
        }

        public void InsertImage(string imagePath)
        {
            originalBitmap = (Bitmap)pictureBox1.Image;
            Bitmap temp = (Bitmap)originalBitmap.Clone();
            Bitmap bmap = (Bitmap)temp.Clone();
            Graphics gr = Graphics.FromImage(bmap);

            Rectangle ScaledCropRect = new Rectangle();
            ScaledCropRect.X = (int)(CropRect.X / ZoomedRatio);
            ScaledCropRect.Y = (int)(CropRect.Y / ZoomedRatio);
            ScaledCropRect.Width = (int)((double)(CropRect.Width) / ZoomedRatio);
            ScaledCropRect.Height = (int)((double)(CropRect.Height) / ZoomedRatio);

            if (!string.IsNullOrEmpty(imagePath))
            {
                Bitmap i_bitmap = (Bitmap)Bitmap.FromFile(imagePath);
                //Rectangle CropRect1 = new Rectangle(xPosition, yPosition, i_bitmap.Width, i_bitmap.Height);
                gr.DrawImage(Bitmap.FromFile(imagePath), ScaledCropRect);
            }
            originalBitmap = (Bitmap)bmap.Clone();
            pictureBox1.Image = originalBitmap;
            pictureBox1.Refresh();

        }

        public string DisplayImagePath
        {
            get { return textBoxImage.Text; }
            set { textBoxImage.Text = value.ToString(); }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            InsertImage(DisplayImagePath);
            checkBoxInsertImage.Checked = false;
            textBoxImage.Text = "";
            Cursor = Cursors.Default;
        }

        private void label19_Click(object sender, EventArgs e)
        {

        }

        private void ApplyTransformRotate(bool preview)
        {
            if (previewBitmap == null)
            {
                return;
            }

            Bitmap selectedSource = null;
            Bitmap bitmapResult = null;

            if (preview == true)
            {
                selectedSource = previewBitmap;
            }
            else
            {
                selectedSource = originalBitmap;
            }

            if (selectedSource != null)
            {
                bitmapResult = selectedSource.RotateImage((double)numericBlue.Value, (double)numericGreen.Value, (double)numericRed.Value);
            }

            if (bitmapResult != null)
            {
                if (preview == true)
                {
                    pictureBox1.Image = bitmapResult;
                }
                else
                {
                    resultBitmap = bitmapResult;
                }
            }
        }


        private void numericBlue_ValueChanged(object sender, EventArgs e)
        {
            ApplyTransformRotate(true);
        }

        private void numericGreen_ValueChanged(object sender, EventArgs e)
        {
            ApplyTransformRotate(true);
        }

        private void numericRed_ValueChanged(object sender, EventArgs e)
        {
            ApplyTransformRotate(true);
        }

        private void panelTransformRotation_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label24_Click(object sender, EventArgs e)
        {

        }

        private void panelInsertText_Paint(object sender, PaintEventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void label26_Click(object sender, EventArgs e)
        {

        }

        private void comboBoxFont_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void comboBoxFontSize_SelectedIndexChanged_2(object sender, EventArgs e)
        {

        }

        private void label28_Click(object sender, EventArgs e)
        {

        }

        private void comboBoxFontStyle_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void comboBoxColor1_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        public void InsertText(string text, int xPosition, int yPosition, string fontName, float fontSize, string fontStyle, string colorName1, string colorName2)
        {
            imagehandler.CurrentBitmap = (Bitmap)pictureBox1.Image;
            Bitmap temp = (Bitmap)imagehandler.CurrentBitmap.Clone();
            Bitmap bmap = (Bitmap)temp.Clone();
            Graphics gr = Graphics.FromImage(bmap);
            if (string.IsNullOrEmpty(fontName))
                fontName = "Times New Roman";
            if (fontSize.Equals(null))
                fontSize = 10.0F;
            Font font = new Font(fontName, fontSize);
            if (!string.IsNullOrEmpty(fontStyle))
            {
                FontStyle fStyle = FontStyle.Regular;
                switch (fontStyle.ToLower())
                {
                    case "bold":
                        fStyle = FontStyle.Bold;
                        break;
                    case "italic":
                        fStyle = FontStyle.Italic;
                        break;
                    case "underline":
                        fStyle = FontStyle.Underline;
                        break;
                    case "strikeout":
                        fStyle = FontStyle.Strikeout;
                        break;

                }
                font = new Font(fontName, fontSize, fStyle);
            }
            if (string.IsNullOrEmpty(colorName1))
                colorName1 = "Black";
            if (string.IsNullOrEmpty(colorName2))
                colorName2 = colorName1;
            Color color1 = Color.FromName(colorName1);
            Color color2 = Color.FromName(colorName2);
            int gW = (int)(text.Length * fontSize);
            gW = gW == 0 ? 10 : gW;
            LinearGradientBrush LGBrush = new LinearGradientBrush(new Rectangle(0, 0, gW, (int)fontSize), color1, color2, LinearGradientMode.Vertical);
            gr.DrawString(text, font, LGBrush, xPosition, yPosition);
            imagehandler.CurrentBitmap = (Bitmap)bmap.Clone();
            pictureBox1.Image = imagehandler.CurrentBitmap;
            pictureBox1.Refresh();
        }

        public int XPosition
        {
            get
            {
                if (string.IsNullOrEmpty(textBoxX.Text))
                    textBoxX.Text = "0";
                return Convert.ToInt32(textBoxX.Text);
            }
            set { textBoxX.Text = value.ToString(); }
        }

        public int YPosition
        {
            get
            {
                if (string.IsNullOrEmpty(textBoxY.Text))
                    textBoxY.Text = "0";
                return Convert.ToInt32(textBoxY.Text);
            }
            set { textBoxY.Text = value.ToString(); }
        }

        public string DisplayText
        {
            get { return textBoxText.Text; }
            set { textBoxText.Text = value.ToString(); }
        }

        public string DisplayTextFont
        {
            get { return comboBoxFont.Text; }
            set { comboBoxFont.Text = value.ToString(); }
        }

        public float DisplayTextFontSize
        {
            get
            {
                float fs = 10.0F;
                if (!string.IsNullOrEmpty(comboBoxFontSize.Text))
                    fs = Convert.ToSingle(comboBoxFontSize.Text.Replace("pt", ""));
                return fs;
            }
            set { comboBoxFont.Text = value.ToString() + "pt"; }
        }

        public string DisplayTextFontStyle
        {
            get { return comboBoxFontStyle.Text; }
            set { comboBoxFontStyle.Text = value.ToString(); }
        }

        public string DisplayTextForeColor1
        {
            get { return comboBoxColor1.Text; }
            set { comboBoxColor1.Text = value.ToString(); }
        }

        public string DisplayTextForeColor2
        {
            get { return comboBoxColor2.Text; }
            set { comboBoxColor2.Text = value.ToString(); }
        }

        private void buttonInsert_Click(object sender, EventArgs e)
        {
            InsertText(DisplayText, XPosition, YPosition, DisplayTextFont, DisplayTextFontSize, DisplayTextFontStyle, DisplayTextForeColor1, DisplayTextForeColor2);
            pictureBox1.Invalidate();
        }

        private void checkBoxGradiant_CheckedChanged(object sender, EventArgs e)
        {
            comboBoxColor2.Enabled = checkBoxGradiant.Checked;
        }

        private void toolCrop_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                SetSize();
                panelRight.Visible = true;
                splitContainer1.Visible = true;
                panelCrop.Visible = true;
                panelContrast.Visible = false;
                panelColorFilter.Visible = false;
                panelDistortion.Visible = false;
                panelColorEffect.Visible = false;
                panelInsertImage.Visible = false;
                panelInsertText.Visible = false;
                panelTransformRotation.Visible = false;

            }
        }

        private void toolRotate_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                cropcheckBox.Checked = false;

                panelLeft.Visible = true;
                panelRotate.Visible = true;
                panelColorToolbar.Visible = false;
            }
        }

        private void toolColor_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                cropcheckBox.Checked = false;
                panelLeft.Visible = true;
                panelRotate.Visible = true;
                panelColorToolbar.Visible = true;

            }
        }

        private void toolText_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                cropcheckBox.Checked = false;
                checkBoxInsertImage.Checked = false;
                panelRight.Visible = true;
                splitContainer1.Visible = true;
                panelCrop.Visible = true;
                panelContrast.Visible = true;
                panelColorFilter.Visible = true;
                panelDistortion.Visible = true;
                panelColorEffect.Visible = true;
                panelInsertImage.Visible = true;
                panelTransformRotation.Visible = true;
                panelInsertText.Visible = true;


            }
        }

        private void toolImage_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                cropcheckBox.Checked = false;

                panelRight.Visible = true;
                splitContainer1.Visible = true;
                panelCrop.Visible = true;
                panelContrast.Visible = true;
                panelColorFilter.Visible = true;
                panelDistortion.Visible = true;
                panelColorEffect.Visible = true;
                panelInsertImage.Visible = true;
                panelTransformRotation.Visible = false;
                panelInsertText.Visible = false;


            }
        }

        private void buttonContrast_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                cropcheckBox.Checked = false;
                panelRight.Visible = true;
                splitContainer1.Visible = true;
                panelCrop.Visible = true;
                panelContrast.Visible = true;
                panelColorFilter.Visible = false;
                panelDistortion.Visible = false;
                panelColorEffect.Visible = false;
                panelInsertImage.Visible = false;
                panelInsertText.Visible = false;
                panelTransformRotation.Visible = false;

            }
        }

        private void buttonOtherEffects_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                cropcheckBox.Checked = false;
                checkBoxInsertImage.Checked = false;
                panelRight.Visible = true;
                splitContainer1.Visible = true;
                panelCrop.Visible = true;
                panelContrast.Visible = true;
                panelColorFilter.Visible = true;
                panelDistortion.Visible = true;
                panelColorEffect.Visible = true;
                panelInsertImage.Visible = false;
                panelTransformRotation.Visible = false;
                panelInsertText.Visible = false;

            }
        }

        private void buttonColorFilter_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                cropcheckBox.Checked = false;
                panelRight.Visible = true;
                splitContainer1.Visible = true;
                panelCrop.Visible = true;
                panelContrast.Visible = true;
                panelColorFilter.Visible = true;
                panelDistortion.Visible = false;
                panelColorEffect.Visible = false;
                panelInsertImage.Visible = false;
                panelInsertText.Visible = false;
                panelTransformRotation.Visible = false;


            }
        }

        private void buttonColorBalance_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                cropcheckBox.Checked = false;
                panelRight.Visible = true;
                splitContainer1.Visible = true;
                panelCrop.Visible = true;
                panelContrast.Visible = true;
                panelColorFilter.Visible = true;
                panelDistortion.Visible = false;
                panelColorEffect.Visible = false;
                panelInsertImage.Visible = false;
                panelInsertText.Visible = false;
                panelTransformRotation.Visible = false;

            }
        }

        private void buttonDistrortion_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                cropcheckBox.Checked = false;
                checkBoxInsertImage.Checked = false;
                panelRight.Visible = true;
                splitContainer1.Visible = true;
                panelCrop.Visible = true;
                panelContrast.Visible = true;
                panelColorFilter.Visible = true;
                panelDistortion.Visible = true;
                panelColorEffect.Visible = false;
                panelInsertImage.Visible = false;
                panelTransformRotation.Visible = false;
                panelInsertText.Visible = false;

            }
        }

        private void trackBarNoise_Scroll_1(object sender, EventArgs e)
        
        {
            b_track = trackBarNoise.Value;
            labelNoise.Text = b_track.ToString();
            Bitmap temp = new Bitmap(originalBitmap);
            Brighten(temp, -b_track);
            pictureBox1.Image = temp;

        }

        private void trackBarContrast_Scroll(object sender, EventArgs e)
        {
            b_track = trackBarContrast.Value;
            labelContrast.Text = b_track.ToString();
            Bitmap temp = new Bitmap(originalBitmap);
            Brighten(temp, -b_track);
            pictureBox1.Image = temp;
        }

        private void StatusDisplayLabel_Click(object sender, EventArgs e)
        {

        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        
        
    }
}
    

