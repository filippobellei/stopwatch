﻿using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using backend.Models;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
public class VideoCameraController : ControllerBase
{
    // Data needed to access the camera recording
    string authenticationString = "admin:mutina23";
    private string ip = "77.89.51.65";
    HttpClient client = new HttpClient();
    private readonly DataContext context;

    public VideoCameraController(DataContext context)
    {
        this.context = context;
    }

    [HttpGet("saveRecording")]
    public async Task<IActionResult> SaveRecording([FromQuery] string startDate, string startTime, string endDate, string endTime) // Formatting: dates=yyyy-mm-dd  times=hh:mm:ss
    {
        // This method downloads a video from a camera.
        // It first retrieves the necessary information for the download request,
        // then sends the download request and checks if it was successful.

        // get.playback.recordinfo request

        // Setting Authentication header
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
        "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString)));

        string recordInfoURL = $"http://{ip}/sdk.cgi?action=get.playback.recordinfo&chnid=0&stream=0&startTime={startDate}%20{startTime}&endTime={endDate}%20{endTime}";

        var recordInfoResponse = await client.GetAsync(recordInfoURL);

        if (recordInfoResponse.IsSuccessStatusCode)
        {
            var content = await recordInfoResponse.Content.ReadAsStringAsync();

            // Parsing the content of recordInfoResponse 
            Dictionary<string, string> recordInfoValues = Utility.ParseResponse(content);

            // Printing the values of recordInfoResponse

            Console.WriteLine("\nPrinting the keys and values of recordInfoResponse");
            foreach (KeyValuePair<string, string> entry in recordInfoValues)
            {
                Console.WriteLine($"{entry.Key}:{entry.Value}");
            }

            // Retrieving needed values for the get.playback.download request
            string chnid = recordInfoValues["chnid"];
            string sid = recordInfoValues["sid"];
            int cnt = int.Parse(recordInfoValues["cnt"]);

            string relativeFilePath = $"./Data/recordings/";

            for (int i = 0; i < cnt; i++)
            {
                // get.playback.recordinfo requests through curl processes

                Console.WriteLine($"\nDownloading video {i + 1} of {cnt}");
                string cntStartDateTime = recordInfoValues[$"startTime{i}"];
                string cntEndDateTime = recordInfoValues[$"endTime{i}"];

                string recordDownloadURL = $"http://{ip}/sdk.cgi?action=get.playback.download&chnid={chnid}&sid={sid}&streamType=primary&videoFormat=mp4&streamData=1&startTime={cntStartDateTime}&endTime={cntEndDateTime}".Replace(" ", "%20");

                string fileName = $"NVR-S{Utility.FormatDate(startDate)}-{Utility.FormatTime(startTime)}-E{Utility.FormatDate(endDate)}-{Utility.FormatTime(endTime)}_{i + 1}.mp4";
                Console.WriteLine($"Video name: {fileName}");

                // Create a new ProcessStartInfo object
                var startInfo = new ProcessStartInfo
                {
                    FileName = "curl",
                    Arguments = $"--http1.0 --output {relativeFilePath}{fileName} -u {authenticationString}  -v {recordDownloadURL}",
                    // Set UseShellExecute to false. This means the process will be executed in the same process, not a new shell.
                    UseShellExecute = false,
                };

                // Create a new Process object and set its StartInfo property to the previously created startInfo object
                var process = new Process { StartInfo = startInfo };

                // Start the process. This will execute the curl command with the specified arguments.
                process.Start();
                await process.WaitForExitAsync();

                //Recording model generation
                try
                {
                    Recording recording = new Recording()
                    {
                        Name = fileName,
                        Path = Path.GetFullPath(relativeFilePath + fileName),
                        Description = "",
                        Size = new FileInfo(relativeFilePath + fileName).Length,
                        StartDateTime = DateTime.ParseExact(cntStartDateTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture).ToUniversalTime(),
                        EndDateTime = DateTime.ParseExact(cntEndDateTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture).ToUniversalTime(),
                    };
                    recording.Duration = recording.EndDateTime - recording.StartDateTime;

                    // adding recording to the database
                    await context.AddAsync(recording);
                    await context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    return StatusCode(500, e.Message);
                }
            }
            return Ok("Video successfully downloaded");
        }
        else
        {
            return StatusCode((int)recordInfoResponse.StatusCode, recordInfoResponse.ReasonPhrase + "Unable to retrieve recording info");
        }
    }

    [HttpGet("download-recordings/{id}")]
    public async Task<IActionResult> getFile(long id)
    {
        Recording file = new Recording();
        file.Id = 1;
        file.Duration = new TimeSpan(0, 2, 0);
        file.Path = "Aggiungi qui il percorso file";
        FileInfo fileInfo = new FileInfo(file.Path);
        file.Size = fileInfo.Length;
        file.Name = fileInfo.Name;
        Recording file2 = new Recording();

        file2.Id = 2;
        file2.Duration = new TimeSpan(0, 2, 0);
        file2.Path = "Aggiungi qui il percorso file";
        fileInfo = new FileInfo(file2.Path);
        file2.Size = fileInfo.Length;
        file2.Name = fileInfo.Name;

        string path = null;
        string fileName = null;

        switch (id)
        {
            case 1:
                path = file.Path;
                fileName = file.Name;
                break;
            case 2:
                path = file2.Path;
                fileName = file2.Name;
                break;

        };

        if (System.IO.File.Exists(path))
        {
            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(path);

            // Puoi personalizzare il tipo di contenuto e il nome del file nella risposta HTTP
            var fileContentResult = new FileContentResult(fileBytes, "application/octet-stream")
            {
                FileDownloadName = fileName // Sostituisci con il nome che desideri dare al file
            };

            return fileContentResult;
        }
        else
        {
            // Gestisci il caso in cui il file non esiste
            return NotFound();
        }
    }

    [HttpGet("getRecordings")]
    public async Task<IActionResult> GetRecordings()
    {
        try
        {
            var recordings = await context.Recordings.ToListAsync();
            return Ok(recordings);
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }
}
