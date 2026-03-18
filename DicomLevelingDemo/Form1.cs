using System;
using System.Drawing;
using System.Windows.Forms;
using Atalasoft.Imaging;
using Atalasoft.Imaging.Codec;
using Atalasoft.Imaging.Codec.Dicom;
using Atalasoft.Imaging.WinControls;
using System.IO;

namespace DicomLeveling
{
    public partial class Form1 : Form
    {
        Stream _stm;
        DicomImage _dImage;
        DicomDataset _dataSet;
        AtalaImage _image;

        public Form1()
        {
            InitializeComponent();

            // Add the DicomDecoder to the registered decoders
            RegisteredDecoders.Decoders.Add(new DicomDecoder());

            // hook into form closing so we can clean up
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
        }

        void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // close all open items
            CloseCurrentItems();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenImageFileDialog oifd = new OpenImageFileDialog();
            oifd.Title = "Select a Dicom Image";

            if (oifd.ShowDialog() == DialogResult.OK)
            {
                FileStream stm = null;
                try
                {
                    // open the file read-only
                    stm = new FileStream(oifd.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                    // Ensure that the file is a Dicom file
                    ImageDecoder decoder = RegisteredDecoders.GetDecoder(stm);
                    if (decoder != null && decoder is DicomDecoder)
                    {
                        // close anything open
                        CloseCurrentItems();

                        // open new file
                        OpenItems(stm);
                    }
                    else
                    {
                        stm.Close();
                        MessageBox.Show("Please choose a Dicom image instead.");
                    }
                }
                catch
                {
                    if (stm != null)
                        stm.Close();
                }
            }
        }

        void SetSliders()
        {
            windowSlider.Value = _dImage.DefaultWindow;
            levelSlider.Value = _dImage.DefaultLeveling;
        }

        private void OpenItems(Stream stm)
        {
            _stm = stm;
            if (_stm == null)
                return;
            try
            {
                // Get the dataset
                _dataSet = new DicomDataset(_stm);

                // Get the modality image
                _dImage = _dataSet.GetModalityTransformedImage(0);

                // set the sliders to the values in the file
                SetSliders();

                // get initial image, transformed to default window and leveling
                int window = windowSlider.Value;
                if (_dImage.ImageCameFromSignedSamples)
                    window += _dImage.ImageDataShiftedBy;
                _image = _dImage.GetAtalaImage(window, this.levelSlider.Value);
                this.imageViewer1.Image = _image;
            }
            catch (Exception err)
            {
                CloseCurrentItems();
                MessageBox.Show("Unable to open file: " + err.Message);
            }
        }

        private void CloseCurrentItems()
        {
            // Dispose and nullify everything
            if (_image != null)
            {
                this.imageViewer1.Image = null;
                _image.Dispose();
                _image = null;
            }
            if (_dImage != null)
            {
                _dImage.Dispose();
                _dImage = null;
            }
            if (_dataSet != null)
            {
                _dataSet.Dispose();
                _dataSet = null;
            }
            if (_stm != null)
            {
                _stm.Close();
                _stm = null;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void UpdateImage()
        {
            if (_dImage != null && _image != null)
            {
                // get the window
                int window = windowSlider.Value;
                if (_dImage.ImageCameFromSignedSamples)
                    window += _dImage.ImageDataShiftedBy;

                // Get the image transformed by the window and level.
                // NOTE: this variant of GetAtalaImage does NOT allocate a new
                // AtalaImage, but instead writes into the one passed in, so
                // there is no need to call Dispose on the image
                _dImage.GetAtalaImage(_image, window, this.levelSlider.Value);

                // For images that may require a backing store of a more optimal
                // pixel format, Refresh() forces that backing store to be dropped,
                // if necessary, then it will call Invalidate()
                imageViewer1.Refresh();
            }
        }

        private void windowSlider_ValueChanged(object sender, EventArgs e)
        {
            UpdateImage();
        }

        private void levelSlider_ValueChanged(object sender, EventArgs e)
        {
            UpdateImage();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AtalaDemos.AboutBox.About aboutBox = new AtalaDemos.AboutBox.About("About Atalasoft Dicom Leveling Demo", "DotImage Dicom Leveling");
            aboutBox.Description =  "This is a very basic demo that shows how to open and view a DICOM image using our DicomImage and DicomDataset classes to enable Window and Level control in a Winforms app.  \r\n\r\n" + 
                                    "If you're looking for display controls on DICOM images such as zoom, magnify and panning, or if you're looking for examples of viewing metadata on DICOM images, check out our Dicom Viewer Demo.  \r\n\r\n" +
                                    "Requires evaluation or purchased licenses of DotImage Document Imaging or Photo Pro, as well as our Dicom Decoder addon.";
            aboutBox.ShowDialog();
        }
    }
}
