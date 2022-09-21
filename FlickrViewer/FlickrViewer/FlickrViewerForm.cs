// Invoking a web service asynchronously with class WebClient

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace FlickrViewer
{
    public partial class FlickrViewerForm : Form
    {
        // Use your Flickr API key here--you can get one at:
        // http://www.flickr.com/services/apps/create/apply
        private const string Key = "7459ad1cded2b09a6301b074454ad23b";

        private const string FlickrWebServiceUrlTemplate = "https://api.flickr.com/services" +
                                                           "/rest/?method=flickr.photos.search&api_key={0}&tags={1}" +
                                                           "&tag_mode=all&per_page=500&privacy_filter=1";

        private const string FlickrResultUrlTemplate = "http://farm{0}.staticflickr.com/{1}/{2}_{3}.jpg";

        // object used to invoke Flickr web service
        private readonly WebClient _flickrClient = new WebClient();

        private Task<string> _flickrTask = null; // Task<string> that queries Flickr

        public FlickrViewerForm()
        {
            InitializeComponent();
        }

        // initiate asynchronous Flickr search query; 
        // display results when query completes
        private async void searchButton_Click(object sender, EventArgs e)
        {
            // if flickrTask already running, prompt user 
            if (_flickrTask != null &&
                _flickrTask.Status != TaskStatus.RanToCompletion)
            {
                var result = MessageBox.Show(
                    "Cancel the current Flickr search?",
                    "Are you sure?", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                // determine whether user wants to cancel prior search
                if (result == DialogResult.No) return;
                _flickrClient.CancelAsync();
            }

            // Flickr's web service URL for searches
            var flickrUrl = string.Format(FlickrWebServiceUrlTemplate, Key, inputTextBox.Text.Replace(" ", ","));

            imagesListBox.DataSource = null; // remove prior data source
            imagesListBox.Items.Clear(); // clear imagesListBox
            pictureBox.Image = null; // clear pictureBox
            imagesListBox.Items.Add("Loading..."); // display Loading...

            try
            {
                // invoke Flickr web service to search Flick with user's tags
                _flickrTask = _flickrClient.DownloadStringTaskAsync(flickrUrl);

                // await flickrTask then parse results with XDocument and LINQ
                XDocument flickrXml = XDocument.Parse( await _flickrTask);

                // gather information on all photos
                var flickrPhotos =
                    from photo in flickrXml.Descendants("photo")
                    let id = photo.Attribute("id").Value
                    let title = photo.Attribute("title").Value
                    let secret = photo.Attribute("secret").Value
                    let server = photo.Attribute("server").Value
                    let farm = photo.Attribute("farm").Value
                    select new FlickrResult
                    {
                        Url = string.Format(FlickrResultUrlTemplate, farm, server, id, secret)
                    };
                imagesListBox.Items.Clear();
                // set ListBox properties only if results were found
                List<FlickrResult> flickrResults = flickrPhotos.ToList();
                if (flickrResults.Any())
                {
                    imagesListBox.DataSource = flickrResults;
                    imagesListBox.DisplayMember = "Title";
                }
                else imagesListBox.Items.Add("No matches");
            }
            catch (WebException)
            {
                if (_flickrTask != null && _flickrTask.Status == TaskStatus.Faulted)
                    MessageBox.Show("Unable to get results from Flickr", "Flickr Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                imagesListBox.Items.Clear();
                imagesListBox.Items.Add("Error occurred");
            }
        }

        // display selected image
        private async void imagesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (imagesListBox.SelectedItem != null)
            {
                string selectedUrl = ((FlickrResult)imagesListBox.SelectedItem).Url;

                // use WebClient to get selected image's bytes asynchronously
                WebClient imageClient = new WebClient();
                byte[] imageBytes = await imageClient.DownloadDataTaskAsync(selectedUrl);

                // display downloaded image in pictureBox
                MemoryStream memoryStream = new MemoryStream(imageBytes);
                pictureBox.Image = Image.FromStream(memoryStream);
            }
        }
    }
}