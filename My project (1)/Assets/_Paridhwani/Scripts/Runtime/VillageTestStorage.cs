using System;
using System.IO;
using UnityEngine;

namespace Paridhwani
{
    // A development-only smoke run may own an explicit writable artifact directory.
    // Normal visits keep using the platform's persistent save location.
    public static class VillageTestStorage
    {
        public static string Cache
        {
            get
            {
                var args=Environment.GetCommandLineArgs();
                if(Debug.isDebugBuild && Array.IndexOf(args,"-village-smoke")>=0)
                {
                    int index=Array.IndexOf(args,"-village-test-directory");
                    if(index>=0 && index+1<args.Length && !args[index+1].StartsWith("-"))return Path.GetFullPath(args[index+1]);
                }
                return Application.temporaryCachePath;
            }
        }
    }
}
