using UrPulse.Client;

Console.WriteLine("=== Ur Labs: Ur Pulse Telegram Test ===");

// 1. أدخل هنا البيانات الحقيقية التي حصلت عليها من تليغرام
string myBotToken = "8872261192:AAHtkgvTJrTiouiXfcrUkWqlOdNdw8GFS88";
string myChatId = "382008027";



// 2. تشغيل العميل
using var pulseClient = new UrPulseClient(
    serverUrl: "http://localhost:5252",
    appId: "vector-kanban",
    serviceName: "Auth-Service",
    intervalSeconds: 5,
    telegramBotToken: myBotToken,
    telegramChatId: myChatId
);

pulseClient.Start();

Console.WriteLine("Press any key to simulate a CRASH and test Telegram Alert...");
Console.ReadKey();

// إيقاف العميل فوراً لبدء الـ 60 ثانية الخاصة بالتصعيد
pulseClient.Stop();
Console.WriteLine("Sample stopped. Server countdown started. Keep an eye on your Telegram app! After 60 seconds, the bot will speak!");

Console.ReadKey();