using Google.Apis.Sheets.v4;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using static System.Environment;

namespace YF
{
    public class Handler
    {
        public class Request
        {
            public string body { get; set; }
        }
        public class Response
        {
            public int statusCode { get; set; }

            public Response(int statusCode = 200)
            {
                this.statusCode = statusCode;
            }
        }
        private class Duty
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Name { get; set; }
            public string TelegramLogin { get; set; }
        }

        public async Task<Response> FunctionHandler(Request request)
        {
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");

            var s = new SpreadsheetsResource(new SheetsService(new Google.Apis.Services.BaseClientService.Initializer
            {
                ApiKey = GetEnvironmentVariable("GOOGLE_API_KEY")
            }));
            var valuesResult = await s.Values.Get(GetEnvironmentVariable("SHEET_ID"), "A2:D2000").ExecuteAsync();
            var list = valuesResult.Values.Select(row => new Duty
            {
                Start = DateTime.Parse((string)row[0]),
                End = DateTime.Parse((string)row[1]),
                Name = (string)row[2],
                TelegramLogin = row.ElementAtOrDefault(3)?.ToString().TrimStart('@')
            });

            var today = DateTime.Today;
            var closeDuties = list.SkipWhile(x => x.End.AddDays(7) < today).Take(3).ToList();
            var previous = closeDuties[0];
            var current = closeDuties[1];

            var telegram = new TelegramBotClient(GetEnvironmentVariable("BOT_TOKEN"));
            var chatId = long.Parse(GetEnvironmentVariable("CHAT_ID"));

            await telegram.SendTextMessageAsync(chatId, $"Спасибо {previous.Name} (@{previous.TelegramLogin}) за дежурство");
            await telegram.SendTextMessageAsync(chatId, $"С <b>{current.Start:d}</b> по <b>{current.End:d}</b> дежурный {current.Name} (@{current.TelegramLogin}), да пребудет с тобой сила",
                Telegram.Bot.Types.Enums.ParseMode.Html);

            return new Response();

        }
    }
}
