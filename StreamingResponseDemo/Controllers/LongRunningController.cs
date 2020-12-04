using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StreamingResponseDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LongRunningController
        : ControllerBase
    {

        private readonly ILogger<LongRunningController> _logger;

        public LongRunningController(ILogger<LongRunningController> logger)
        {
            _logger = logger;
        }


        [HttpGet("demo1")]
        public async Task GetDemo1Async()
        {
            var riga = new double[1000];

            this.Response.StatusCode = 200;
            this.Response.Headers.Add(HeaderNames.ContentType, "text/plain");
            var outputStream = this.Response.Body;
            var buffer = new byte[0x10000];
            for (int i = 0; i < 50000; i++)
            {
                //elaborazione riga i-esima
                for (int k = 0; k < riga.Length; k++)
                {
                    riga[k] = Math.PI * i * k;
                }

                string s = JsonSerializer.Serialize(riga);
                int byteCount = Encoding.UTF8.GetBytes(s, 0, s.Length, buffer, 0);
                buffer[byteCount] = (byte)'\r';
                await outputStream.WriteAsync(buffer, 0, byteCount + 1);
            }
            await outputStream.FlushAsync();
        }


        [HttpGet("demo2")]
        public async Task GetDemo2Async()
        {
            var riga = new double[1000];

            var writer = new MyStreamingWriter(this.HttpContext);
            try
            {
                await writer.BeginFieldAsync("nome");
                await writer.WriteAsync("Mario");

                await writer.BeginFieldAsync("nato");
                await writer.WriteAsync(new DateTime(1966, 7, 23));

                await writer.BeginFieldAsync("campo_nullo");
                await writer.WriteAsync(null);

                await writer.BeginFieldAsync("matrice");
                const int N = 10000;
                for (int i = 0; i < N; i++)
                {
                    for (int k = 0; k < riga.Length; k++)
                    {
                        riga[k] = Math.PI * i * k;
                    }
                    await writer.WriteAsync(riga);
                }

                await writer.BeginFieldAsync("count");
                await writer.WriteAsync(N);
            }
            finally
            {
                await writer.EndAsync();
            }
        }


        [HttpPut("demo3")]
        public async Task PutDemo3Async(
            MyPutPayload payload
            )
        {
            Console.WriteLine($"{nameof(MyPutPayload.Booleano)}={payload.Booleano}");
            Console.WriteLine($"{nameof(MyPutPayload.Intero)}={payload.Intero}");
            Console.WriteLine($"{nameof(MyPutPayload.Reale)}={payload.Reale}");
            Console.WriteLine($"{nameof(MyPutPayload.Testo)}={payload.Testo}");
            Console.WriteLine($"{nameof(MyPutPayload.Istante)}={payload.Istante}");

            var riga = new double[1000];

            var writer = new MyStreamingWriter(this.HttpContext);
            try
            {
                await writer.BeginFieldAsync("nome");
                await writer.WriteAsync("Mario");

                await writer.BeginFieldAsync("nato");
                await writer.WriteAsync(new DateTime(1966, 7, 23));

                await writer.BeginFieldAsync("campo_nullo");
                await writer.WriteAsync(null);

                await writer.BeginFieldAsync("matrice");
                const int N = 10000;
                for (int i = 0; i < N; i++)
                {
                    for (int k = 0; k < riga.Length; k++)
                    {
                        riga[k] = Math.PI * i * k;
                    }
                    await writer.WriteAsync(riga);
                }

                await writer.BeginFieldAsync("count");
                await writer.WriteAsync(N);
            }
            finally
            {
                await writer.EndAsync();
            }
        }

        public class MyPutPayload
        {
            public bool Booleano { get; set; }
            public int Intero { get; set; }
            public double Reale { get; set; }
            public string Testo { get; set; }
            public DateTime Istante { get; set; }
        }

    }
}
