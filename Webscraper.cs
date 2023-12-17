using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Collections.ObjectModel;
using System.Globalization;
using CsvHelper;


namespace WebScraping
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string website = "";

            while (website.Length <= 0 && website != "Y" && website != "I" && website != "W")
            {
                if (args.Length > 0)
                {
                    website = args[0];
                }
                else
                {
                    Console.WriteLine("Do you want to scrape YouTube[Y] or ictJobs.be[I] or Wikipedia[W]");
                    website = Console.ReadLine().Trim().ToUpper();
                }
            }
            
            String query = "";
            while (query.Length <= 0)
            {
                if (args.Length > 0)
                {
                    query = args[0];
                }
                else
                {
                    Console.WriteLine("Enter the query:");
                    query = Console.ReadLine().Trim();
                }
            }

            Scraper scraper = new Scraper();
            scraper.startBrowser();
            if (website == "Y") scraper.YouTubeScraping(query);
            if (website == "I") scraper.ICTJobScraping(query);
            if (website == "W") scraper.WikipediaScraping(query);
            scraper.close_Browser();
        }
    }


    public class Scraper
    {
        public IWebDriver driver;

        public void startBrowser()
        {
            /* Local Selenium WebDriver */
            ChromeOptions options = new ChromeOptions();
            options.BrowserVersion = "120.0";
            
            /* Initialize the driver */
            driver = new ChromeDriver(options);
            driver.Manage().Window.Maximize();
        }

        public class YouTubeResult
        {
            public string Link { get; set; }
            public string Title { get; set; }
            public string Uploader { get; set; }
            public string Views { get; set; }
            public string ReleaseDate { get; set; }
        }

        public void YouTubeScraping(string query)
        {
            query = query.Replace(" ", "+");
            String url = "https://www.youtube.com/results?search_query=" + query;
            driver.Url = url;
            /* Explicit Wait to ensure that the page is loaded completely by reading the DOM state */
            var timeout = 10000; 
            var wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(timeout));
            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

            /* Click 'reject all' on the cookie banner */
            IWebElement cookieBanner = driver.FindElement(By.XPath("//*[@id='dialog']"));
            IWebElement rejectAllButton = cookieBanner.FindElement(By.XPath("//*[@id=\"content\"]/div[2]/div[6]/div[1]/ytd-button-renderer[1]/yt-button-shape/button/yt-touch-feedback-shape/div/div[2]"));
            rejectAllButton.Click();

            /* Go to the 'Recently Uploaded' tab */
            IWebElement chips = driver.FindElement(By.XPath("//*[@id=\"chips\"]"));
            ReadOnlyCollection<IWebElement> chipElements = chips.FindElements(By.XPath("./*"));

            foreach (IWebElement chipElement in chipElements)
            {
                if (chipElement.Text == "Recently uploaded")
                {
                    chipElement.Click();
                    break;
                }
            }

            /* Create a Videos list with all videos it can find on the page */
            By elem_videos = By.XPath("//*[@id=\"dismissible\"]");
            ReadOnlyCollection<IWebElement> videos = driver.FindElements(elem_videos);

            Int32 vcount = 1;
            List<YouTubeResult> results = new List<YouTubeResult>();
            /* Go through the Videos List and get the attributes of the videos */
            foreach (IWebElement video in videos)
            {
                string str_link, str_title, str_upl, str_views, str_rel;
                IWebElement elem_video_link = video.FindElement(By.XPath(".//a[@id=\"video-title\"]"));
                str_link = elem_video_link.GetAttribute("href");

                IWebElement elem_video_title = video.FindElement(By.CssSelector("#video-title"));
                str_title = elem_video_title.Text;

                IWebElement elem_video_upl_field = video.FindElement(By.XPath(".//*[@id='channel-name']//a"));
                str_upl = elem_video_upl_field.GetAttribute("textContent").Trim();

                IWebElement elem_video_views = video.FindElement(By.XPath(".//*[@id='metadata-line']/span[1]"));
                str_views = elem_video_views.Text;

                IWebElement elem_video_reldate = video.FindElement(By.XPath(".//*[@id='metadata-line']/span[2]"));
                str_rel = elem_video_reldate.Text;

                Console.WriteLine("******* Video " + vcount + " *******");
                Console.WriteLine("Video Link: " + str_link);
                Console.WriteLine("Video Title: " + str_title);
                Console.WriteLine("Video Uploader: " + str_upl);
                Console.WriteLine("Video Views: " + str_views);
                Console.WriteLine("Video Release Date: " + str_rel);
                Console.WriteLine("\n");

                /* Add results to the results list */
                results.Add(new YouTubeResult { Link = str_link, Title= str_title, Uploader = str_upl, Views = str_views, ReleaseDate = str_rel });
                
                /* Only show five videos */
                if (vcount == 5) break;
                vcount++;
             }

            string csvFilePath = "./results/output.csv";

            using (var writer = new StreamWriter(csvFilePath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(results);
            }

            Console.WriteLine("Results written to " + csvFilePath);
        }

        public class ICTJobsResult
        {
            public string Title { get; set; }
            public string Company { get; set; }
            public string Location { get; set; }
            public string Keywords { get; set; }
            public string Link { get; set; }
        }

        public void ICTJobScraping(String query)
        {
            String url = "https://www.ictjob.be/en/search-it-jobs?query=keyword%3A%3A" + query;
            driver.Url = url;
            /* Explicit Wait to ensure that the page is loaded completely by reading the DOM state */
            var timeout = 10000;
            var wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(timeout));
            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

            Thread.Sleep(3000);

            /* Order by data */
            IWebElement dateButton = driver.FindElement(By.XPath("//*[@id=\"sort-by-date\"]"));

            /* Create a list with all job listings it can find */
            ReadOnlyCollection<IWebElement> jobs = driver.FindElements(By.XPath("//*[@id=\"search-result-body\"]/div/ul/li"));

            Int32 jcount = 1;
            List<ICTJobsResult> results = new List<ICTJobsResult>();
            foreach (IWebElement job in jobs) 
            {
                string str_title, str_company, str_location, str_keywords, str_link;
                try
                {
                    IWebElement elem_job_title = job.FindElement(By.XPath(".//h2[@class='job-title']"));
                    str_title = elem_job_title.Text;

                    IWebElement elem_job_company = job.FindElement(By.XPath(".//span[@class='job-company']"));
                    str_company = elem_job_company.Text;

                    IWebElement elem_job_location = job.FindElement(By.XPath(".//span[@class='job-location']"));
                    str_location = elem_job_location.Text;

                    IWebElement elem_job_keywords = job.FindElement(By.XPath(".//span[@class='job-keywords']"));
                    str_keywords = elem_job_keywords.Text;

                    IWebElement elem_job_link = job.FindElement(By.XPath(".//a[@class='job-title search-item-link']"));
                    str_link = elem_job_link.GetAttribute("href");

                    Console.WriteLine("******* Job " + jcount + " *******");
                    Console.WriteLine("Job title: " + str_title);
                    Console.WriteLine("Job company: " + str_company);
                    Console.WriteLine("Job location: " + str_location);
                    Console.WriteLine("Job keywords: " + str_keywords);
                    Console.WriteLine("Link to listing: " + str_link);

                    /* Add results to the results list */
                    results.Add(new ICTJobsResult { Title = str_title, Company = str_company, Location = str_location, Keywords = str_keywords, Link = str_link });

                    if (jcount == 5) break;
                    jcount++;
                }
                catch (Exception ex) 
                {
                    continue;
                }   
            }

            string csvFilePath = "./results/output.csv";

            using (var writer = new StreamWriter(csvFilePath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(results);
            }

            Console.WriteLine("Results written to " + csvFilePath);

        }

        public class WikipediaResult
        {
            public string Title { get; set; }
            public string Summary { get; set; }
            public List<string> References { get; set; } = new List<string>();

            public void AddReference(string reference)
            {
                References.Add(reference);
            }
        }

        public void WikipediaScraping(String query) 
        {
            query = query.Replace(" ", "_");
            String url = "https://en.wikipedia.org/wiki/" + query;
            driver.Url = url;
            /* Explicit Wait to ensure that the page is loaded completely by reading the DOM state */
            var timeout = 10000;
            var wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(timeout));
            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

            /* Create an element with all the content from the page */
            IWebElement article = driver.FindElement(By.XPath("/html/body/div[2]/div/div[3]"));

            string str_title, str_summ;
            IWebElement elem_article_title = article.FindElement(By.XPath(".//*[@id=\"firstHeading\"]/span"));
            str_title= elem_article_title.Text;

            IWebElement elem_article_summ = article.FindElement(By.XPath(".//*[@id=\"mw-content-text\"]/div[1]/p[2]"));
            str_summ = elem_article_summ.Text;

            Console.WriteLine("Article title: " + str_title);
            Console.WriteLine("Article summary: " + str_summ);

            WikipediaResult result = new WikipediaResult { Title = str_title, Summary = str_summ };

            /* Get a list with all references */
            By elem_refs = By.XPath("//*[@id=\"mw-content-text\"]/div[1]/div[10]/ol/li");
            ReadOnlyCollection<IWebElement> refs = driver.FindElements(elem_refs);

            Console.WriteLine("Total number of references: " + refs.Count);
            Console.WriteLine("First five references:");

            /* Print the first five references */
            Int32 rcount = 1;
            string str_ref;
            foreach (IWebElement reference in refs)
            {
                ReadOnlyCollection<IWebElement> elem_ref = reference.FindElements(By.XPath(".//a"));
                str_ref = elem_ref[1].GetAttribute("href");
                Console.WriteLine("Reference " + rcount + ": " + str_ref);

                /* Add results to the results list */
                result.References.Add(str_ref);

                if (rcount == 5) break;
                rcount++;
            }

            string csvFilePath = "./results/output.csv";

            using (var writer = new StreamWriter(csvFilePath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(new List<WikipediaResult> { result });
            }

            Console.WriteLine("Results written to " + csvFilePath);
        }

        public void close_Browser()
        {
            driver.Quit();
        }
    }
}