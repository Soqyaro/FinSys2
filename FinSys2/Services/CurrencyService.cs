using System.Xml.Linq;
using System.Text;

namespace FinSys2.Services
{
    public class CurrencyService
    {
        private const string Url = "https://www.cbr.ru/scripts/XML_daily.asp";//ЦБ РФ

        public async Task<Dictionary<string, string>> GetExchangeRates()
        {
            try
            {
                //регистрация поддержки старых кодировок (windows-1251)
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                using var client = new HttpClient();

                //заголовок
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                //получеине данных как массив байтов
                var responseBytes = await client.GetByteArrayAsync(Url);

                //декодер байтов в строку, используя кодировку
                var xmlString = Encoding.GetEncoding("windows-1251").GetString(responseBytes);

                var xdoc = XDocument.Parse(xmlString);

                var rates = new Dictionary<string, string>();
                var codes = new[] { "USD", "EUR", "CNY", "KZT", "TRY", "AED" };

                foreach (var code in codes)
                {
                    var valute = xdoc.Descendants("Valute")
                        .FirstOrDefault(x => x.Element("CharCode")?.Value == code);

                    if (valute != null)
                    {
                        var value = valute.Element("Value")?.Value;
                        //ЦБ курс к красивому виду
                        rates.Add(code, value + " ₽");
                    }
                }
                return rates;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка валют: " + ex.Message);
                return new Dictionary<string, string> { { "Ошибка", "Сервер недоступен" } };
            }
        }
    }
}