using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace CrosshairZ.Interop
{
    public static class RustInterop
    {
        private const string DllName = "Native\\x64\\crosshair_ffi.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern int chz_handle_request(
           IntPtr inputJson,
           out IntPtr outputJson,
           out IntPtr errorJson);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern void chz_free_string(IntPtr ptr);

        public static IReadOnlyList<DrawCmd> BuildPreview(CrosshairData data, float width, float height)
        {
            var token = InvokeRaw(Requests.BuildPreview(data, width, height));
            if (token["Preview"] is JArray arr)
            {
                return arr.ToObject<List<DrawCmd>>();
            }

            return Array.Empty<DrawCmd>();
        }

        public static string EncodeShareCode(CrosshairData data)
        {
            var token = InvokeRaw(Requests.EncodeShareCode(data));
            return token["ShareCode"]?.ToObject<string>() ?? string.Empty;
        }

        public static CrosshairData DecodeShareCode(string code)
        {
            var token = InvokeRaw(Requests.DecodeShareCode(code));
            return token["Decoded"]?.ToObject<CrosshairData>();
        }

        public static CrosshairData Normalize(CrosshairData data)
        {
            var token = InvokeRaw(Requests.SetActiveCrosshair(data));
            return token["ActiveProfile"]?.ToObject<CrosshairData>() ?? data;
        }

        private static JObject InvokeRaw(object request)
        {
            string json = JsonConvert.SerializeObject(request);

            IntPtr inputPtr = IntPtr.Zero;
            IntPtr outputPtr = IntPtr.Zero;
            IntPtr errorPtr = IntPtr.Zero;

            try
            {
                inputPtr = Marshal.StringToHGlobalAnsi(json);

                int rc = chz_handle_request(inputPtr, out outputPtr, out errorPtr);

                string output = PtrToStringUtf8OrAnsi(outputPtr);
                string error = PtrToStringUtf8OrAnsi(errorPtr);

                if (rc != 0)
                {
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? $"Rust FFI failed with code {rc}" : error);
                }

                if (string.IsNullOrWhiteSpace(output))
                {
                    throw new InvalidOperationException("Rust FFI returned empty output.");
                }

                return JObject.Parse(output);
            }
            finally
            {
                if (inputPtr != IntPtr.Zero) Marshal.FreeHGlobal(inputPtr);
                if (outputPtr != IntPtr.Zero) chz_free_string(outputPtr);
                if (errorPtr != IntPtr.Zero) chz_free_string(errorPtr);
            }
        }

        private static string PtrToStringUtf8OrAnsi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;

            var bytes = new List<byte>();
            int offset = 0;
            while (true)
            {
                byte b = Marshal.ReadByte(ptr, offset++);
                if (b == 0) break;
                bytes.Add(b);
            }

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

    }
}