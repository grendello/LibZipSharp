﻿//
// Utilities.cs
//
// Author:
//       Marek Habersack <grendel@twistedcode.net>
//
// Copyright (c) 2016 Xamarin, Inc (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Text;
using System.Runtime.InteropServices;

using Mono.Unix.Native;

namespace Xamarin.Tools.Zip
{
	partial class Utilities
	{
		public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc); // ZIP entries use GMT/UTC

		public static bool IsUnix { get; } = Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix;

		public static int Errno {
			get { return Marshal.GetLastWin32Error (); }
		}

		static Utilities ()
		{
		}

		public static string SanitizeFilePath (string filePath)
		{
			if (String.IsNullOrEmpty (filePath))
				return filePath;

			if (filePath.IndexOf ('\\') >= 0)
				filePath = filePath.Replace ('\\', '/');
			if (filePath.IndexOf ('/') < 0)
				return filePath;

			int lastRelative = filePath.LastIndexOf ("../");
			if (lastRelative < 0)
				return filePath;

			return filePath.Substring (lastRelative + 3);
		}

		public static string GetStringFromNativeAnsi (IntPtr data)
		{
			return Marshal.PtrToStringAnsi (data);
		}

		public static DateTime DateTimeFromUnixTime (ulong time)
		{
			try {
				return UnixEpoch.AddSeconds (time);
			} catch (ArgumentOutOfRangeException) {
				// Some ZIPs have timestamps larger than 9218762655527012 which
				// will cause the above code to throw. We'll return the epoch
				// in such instance
				return UnixEpoch;
			}
		}

		public static ulong UnixTimeFromDateTime (DateTime time)
		{
			if (time < UnixEpoch)
				return 0;

			return (ulong)((time - UnixEpoch).TotalSeconds);
		}

		public unsafe static IntPtr StringToUtf8StringPtr (string str)
		{
			if (str == null)
				throw new ArgumentNullException (nameof (str));

			var encoding = Encoding.UTF8;
			int count = encoding.GetByteCount (str);
			IntPtr memory = Marshal.AllocHGlobal (count + 1);
			fixed (char *pStr = str)
				encoding.GetBytes (pStr, str.Length, (byte*)memory, count);
			*(((byte*)memory) + count) = 0;
			return memory;
		}

		public unsafe static string Utf8StringPtrToString (IntPtr utf8Str)
		{
			if (utf8Str == IntPtr.Zero)
				return null;

			byte* ptr = (byte*)utf8Str.ToPointer ();
			byte* p = ptr;
			while (*p != 0)
				p++;
			var len = (int)(p - ptr);
			if (len == 0)
				return String.Empty;

			var bytes = new byte[len];
			Marshal.Copy (utf8Str, bytes, 0, len);
			return Encoding.UTF8.GetString (bytes);
		}

		public static void FreeUtf8StringPtr (IntPtr ptr)
		{
			Marshal.FreeHGlobal (ptr);
		}
	}
}

