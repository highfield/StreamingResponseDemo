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

            var writer = new MyStreamingWriter(this);
            try
            {
                await writer.BeginFieldAsync("nome");
                await writer.WriteAsync("Mario");

                await writer.BeginFieldAsync("nato");
                await writer.WriteAsync(new DateTime(1966, 7, 23));

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
    }

    public class MyStreamingWriter
    {
        public MyStreamingWriter(
            ControllerBase controller
            )
        {
            this._stream = controller.Response.Body;
            this._buffer = new byte[0x10000];

            controller.Response.StatusCode = 200;   //OK
            controller.Response.Headers.Add(HeaderNames.ContentType, "text/plain");
        }

        private readonly Stream _stream;
        private readonly byte[] _buffer;

        public async Task BeginFieldAsync(string name)
        {
            this._buffer[0] = (byte)'\a';
            int byteCount = Encoding.UTF8.GetBytes(name, 0, name.Length, this._buffer, 1);
            //TODO controllo buffer overflow
            this._buffer[byteCount + 1] = (byte)'\r';
            await this._stream.WriteAsync(this._buffer, 0, byteCount + 2);
        }

        public async Task WriteAsync(object content)
        {
            string s = JsonSerializer.Serialize(content);
            int byteCount = Encoding.UTF8.GetBytes(s, 0, s.Length, this._buffer, 0);
            //TODO controllo buffer overflow
            this._buffer[byteCount] = (byte)'\r';
            await this._stream.WriteAsync(this._buffer, 0, byteCount + 1);
        }

        public async Task EndAsync()
        {
            await this._stream.FlushAsync();
        }
    }
}
