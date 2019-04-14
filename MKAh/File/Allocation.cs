//
// MKAh.File.Allocation.cs
//
// Author:
//       M.A. (https://github.com/mkahvi)
//
// Copyright (c) 2018–2019 M.A.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKAh.File
{
	public static partial class Allocation
	{
		/// <summary>
		/// Pre-allocates a file.
		/// Not recommended for files that are written with append mode due to them simply adding to the end of the size set by this.
		/// </summary>
		/// <param name="fullpath">Full path to the file.</param>
		/// <param name="allockb">Size in kB to allocate.</param>
		/// <param name="kib">kiB * 1024; kB * 1000</param>
		/// <param name="writeonebyte">Writes one byte at the new end.</param>
		static public void Prealloc(string fullpath, long allockb = 64, bool kib = true, bool writeonebyte = false)
		{
			Debug.Assert(allockb >= 0);
			Debug.Assert(string.IsNullOrEmpty(fullpath));

			long boundary = kib ? 1024 : 1000;

			System.IO.FileStream fs = null;
			try
			{
				fs = System.IO.File.Open(fullpath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
				var oldsize = fs.Length;

				if (fs.Length < (boundary * allockb))
				{
					// TODO: Make sparse. Unfortunately requires P/Invoke.
					fs.SetLength(boundary * allockb); // 1024 was dumb for HDDs but makes sense for SSDs again.
					if (writeonebyte)
					{
						fs.Seek((boundary * allockb) - 1, SeekOrigin.Begin);
						byte[] nullarray = { 0 };
						fs.Write(nullarray, 0, 1);
					}
					Debug.WriteLine($"<Core> Pre-allocated file: {fullpath} ({(oldsize / boundary).ToString()} kB -> {allockb.ToString()} kB)");
				}
			}
			finally
			{
				fs?.Close();
			}
		}

		const int FSCTL_SET_SPARSE = 0x000900c4;

		/// <summary>
		/// Sparsely pre-allocates a file.
		/// Not recommended for files that are written with append mode due to them simply adding to the end of the size set by this.
		/// </summary>
		/// <param name="fullpath"></param>
		/// <param name="allockb">KiB to sparsely pre-alloc.</param>
		/// <param name="ssd">UNUSED</param>
		static public void SparsePrealloc(string fullpath, long allockb = 64, bool ssd = true)
		{
			int brv = 0;
			var ol = new System.Threading.NativeOverlapped();

			FileStream fs = null;
			try
			{
				fs = System.IO.File.Open(fullpath, FileMode.Open, FileAccess.Write);

				bool rv = NativeMethods.DeviceIoControl(
					fs.SafeFileHandle, FSCTL_SET_SPARSE,
					IntPtr.Zero, 0, IntPtr.Zero, 0, ref brv, ref ol);
				// if (result == false) return false;

				if (rv) fs.SetLength(allockb * 1024);
			}
			catch (Exception ex) when (ex is NullReferenceException || ex is OutOfMemoryException) { throw; }
			//catch { } // ignore, no access probably
			finally
			{
				fs?.Dispose();
			}
		}
	}
}
