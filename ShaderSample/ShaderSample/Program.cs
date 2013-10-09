using System;

namespace ShaderSample
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (ShaderSample game = new ShaderSample())
            {
                game.Run();
            }
        }
    }
#endif
}

