using System.Net;
using System.Text.RegularExpressions;
using FakePhoneNumberParser;

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

    scanner.Scan(new Uri("https://fakenumber.org/"), 50);
}