using System.Net;
using System.Text.RegularExpressions;

using (Scanner scanner = new())
{
    scanner.TargetFound += (page, phoneNumbers, title, nestingLevel) =>
    {
        Console.WriteLine($"\nPage:\t{title}\n\t{page}\t{nestingLevel}\nPhone numbers:");
        foreach (var phoneNumber in phoneNumbers)
        {
            Console.WriteLine($"\t{phoneNumber}");
            File.AppendAllText("phonenumbers.txt", phoneNumber + "\n");
        }
    };

    scanner.Scan(new Uri("https://fakenumber.org/"), 10);
}


class Scanner : IDisposable
{
    public event Action<Uri, string[], string, int> TargetFound;
    private readonly HashSet<Uri> procLinks = new();
    private readonly WebClient webClient = new();
    private readonly HashSet<string> ignoreFiles = new() { ".xml", ".iso" };


    private void OnTargetFound(Uri page, string[] phoneNumbers, string title, int level)
    {
        TargetFound.Invoke(page, phoneNumbers, title, level);
    }

    private void Process(string domain, Uri page, int count, int level)
    {
        if (count <= 0) return;
        if (procLinks.Contains(page)) return;
        if (level >= 10) return;
        procLinks.Add(page);

        string html = webClient.DownloadString(page);
        string title = Regex.Match(html, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase).Groups["Title"].Value;

        var hrefs = Regex.Matches(html, @"href\s*=\s*(?:[""'](?<1>[^""']*)[""']|(?<1>\S+))").Cast<Match>().Select(href =>
        {
            var url = href.Value.Replace("href=", "").Trim('"');
            var loc = url.StartsWith("/");
            try
            {
                var urlAddress = new Uri(loc ? $"{domain}{url}" : url);
                return new
                {
                    Ref = new Uri(loc ? $"{domain}{url}" : url),
                    IsLocal = loc || url.StartsWith(domain)
                };
            }
            catch (UriFormatException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
        }).Where(href => href != null);

        var phoneNumbers = (from phoneNumber in Regex.Matches(html, @"\d{3}-\d{3}-\d{4}").Cast<Match>()
                            let pn = phoneNumber.Value
                            select new 
                            {
                                PhoneNumber = pn
                            }).ToList();

        var phoneNumbersArray = (from phoneNumber in phoneNumbers
                                 select phoneNumber.PhoneNumber).ToArray();

        if (phoneNumbersArray.Length > 0)
        {
            OnTargetFound(page, phoneNumbersArray, title, level);
        }

        var locals = (from href in hrefs
                      where href.IsLocal
                      select href.Ref).ToList();

        foreach (var href in locals)
        {
            string exFile = Path.GetExtension(href.LocalPath).ToLower();
            if (ignoreFiles.Contains(exFile))
            {
                continue;
            }

            Process(domain, href, --count, level + 1);
        }
    }

    public void Scan(Uri startPage, int pageCount)
    {
        procLinks.Clear();
        string domain = $"{startPage.Scheme}://{startPage.Host}";
        int level = 0;

        Process(domain, startPage, pageCount, level);
    }

    public void Dispose()
    {
        webClient.Dispose();
    }
}