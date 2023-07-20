using CacheRedis.Model;
using CacheRedis.Service;
using System.Net.Mail;
using System.Net;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin.Messaging;

string eventQueue = "eventQueues";
string schoolRequesttQueue = "SchoolRequestQueue";
string notiQueue = "notiQueue";

string smtpServer = "smtp.gmail.com";
int smtpPort = 587;
string senderEmail = "ntthuc321@gmail.com";
string senderPassword = "ymqaghxmdaaxaigt";
RedisService redisService = new RedisService("redis-11771.c295.ap-southeast-1-1.ec2.cloud.redislabs.com:11771,password=R4bKNJqwkwzolphEm3aSdPjHwIX4UUK8");
FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile("firebase-config.json"),
});
while (true)
{
    var schoolRequestTask = ProcessSchoolRequests(redisService, schoolRequesttQueue, smtpServer, smtpPort, senderEmail, senderPassword);
    var eventTask = ProcessEvents(redisService, eventQueue);
    var pushNotiTask = ProcessNotication(redisService, notiQueue);
    await Task.WhenAny(schoolRequestTask, eventTask, pushNotiTask);

}




static async Task ProcessSchoolRequests(RedisService redisService, string queueName, string smtpServer, int smtpPort, string senderEmail, string senderPassword)
{
    while (redisService.HasNextMessage(queueName))
    {
        var schoolRequest = redisService.DequeueMessage<SchoolEmailRespone>(queueName);
        if (schoolRequest != null)
        {
            string recipientEmail = schoolRequest.Email;
            string subject = "Response for School Request";
            string body = schoolRequest.Status == 2 ? $"{schoolRequest.SchoolName} was accepted" : $"{schoolRequest.SchoolName} was rejected";

            SendEmailService sendEmailService = new SendEmailService(smtpServer, smtpPort, senderEmail, senderPassword);
            await sendEmailService.SendEmailAsync(recipientEmail, subject, body);
            Console.WriteLine("send to: " + recipientEmail);

        }
    }
}

static async Task ProcessEvents(RedisService redisService, string queueName)
{
    while (redisService.HasNextMessage(queueName))
    {
        var message = redisService.DequeueMessage<Event>(queueName);
        if (message != null)
        {
            await redisService.DeleteAsync($"Event:AlumniId:*");

            var eventSchoolkey = await redisService.GetObjectAllAsync($"Event:SchoolId:{message.SchoolId}:*");
            if (eventSchoolkey != null)
            {
                for (var i = 0; i < eventSchoolkey.Count(); i++)
                {
                    var item = eventSchoolkey[i];
                    await redisService.DeleteAsync(item);
                }

            }
            var alumniSchoolkey = await redisService.GetObjectAllAsync($"Event:alumniId:*");
            if (alumniSchoolkey != null)
            {
                for (var i = 0; i < alumniSchoolkey.Count(); i++)
                {
                    var item = alumniSchoolkey[i];
                    await redisService.DeleteAsync(item);
                }

            }

            Console.WriteLine("Update " + message.SchoolId);
        }
    }
}

static async Task ProcessNotication(RedisService redisService, string queueName)
{


    var message = redisService.DequeueMessage<string>(queueName);
    if (message != null)
    {
        var schoolid = message.Split(",")[0];
        var alumniid = message.Split(",")[1];
        var mess = new Message()
        {
            Topic = schoolid,
            Data = new Dictionary<string, string>()
        {
            {schoolid,alumniid }
        },

            Notification = new Notification()
            {
                Title = "New Request schoolID" + schoolid,
                Body = "New Request alumniID :" + alumniid,
            },
        };
        string response = await FirebaseMessaging.DefaultInstance.SendAsync(mess);
        Console.WriteLine("Successfully send message: " + response);
    }

}