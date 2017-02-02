using System;
using System.IO;
using System.Text;

namespace SDE.Core {
	public class Utf8Checker {
		public static Utf8Checker Instance = new Utf8Checker();

		public bool Check(string fileName) {
			using (BufferedStream fstream = new BufferedStream(File.OpenRead(fileName))) {
				return IsUtf8(fstream);
			}
		}

		/// <summary>
		/// Check if stream is utf8 encoded.
		/// Notice: stream is read completely in memory!
		/// </summary>
		/// <param name="stream">Stream to read from.</param>
		/// <returns>True if the whole stream is utf8 encoded.</returns>
		public bool IsUtf8(Stream stream) {
			int count = 4 * 1024;
			byte[] buffer;
			int read;
			while (true) {
				buffer = new byte[count];
				stream.Seek(0, SeekOrigin.Begin);
				read = stream.Read(buffer, 0, count);
				if (read < count) {
					break;
				}
				count *= 2;
			}
			return IsUtf8(buffer, read);
		}

		public static bool IsUtf8(string line, Encoding current) {
			byte[] buffer = current.GetBytes(line);
			return IsUtf8(buffer, buffer.Length);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public static bool IsUtf8(byte[] buffer, int length) {
			int position = 0;
			int bytes = 0;
			while (position < length) {
				if (!IsValid(buffer, position, length, ref bytes)) {
					return false;
				}
				position += bytes;
			}
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="position"></param>
		/// <param name="length"></param>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static bool IsValid(byte[] buffer, int position, int length, ref int bytes) {
			if (length > buffer.Length) {
				throw new ArgumentException("Invalid length");
			}

			if (position > length - 1) {
				bytes = 0;
				return true;
			}

			byte ch = buffer[position];

			if (ch <= 0x7F) {
				bytes = 1;
				return true;
			}

			if (ch >= 0xc2 && ch <= 0xdf) {
				if (position >= length - 2) {
					bytes = 0;
					return false;
				}
				if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf) {
					bytes = 0;
					return false;
				}
				bytes = 2;
				return true;
			}

			if (ch == 0xe0) {
				if (position >= length - 3) {
					bytes = 0;
					return false;
				}

				if (buffer[position + 1] < 0xa0 || buffer[position + 1] > 0xbf ||
				    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf) {
					bytes = 0;
					return false;
				}
				bytes = 3;
				return true;
			}

			if (ch >= 0xe1 && ch <= 0xef) {
				if (position >= length - 3) {
					bytes = 0;
					return false;
				}

				if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf ||
				    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf) {
					bytes = 0;
					return false;
				}

				bytes = 3;
				return true;
			}

			if (ch == 0xf0) {
				if (position >= length - 4) {
					bytes = 0;
					return false;
				}

				if (buffer[position + 1] < 0x90 || buffer[position + 1] > 0xbf ||
				    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
				    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf) {
					bytes = 0;
					return false;
				}

				bytes = 4;
				return true;
			}

			if (ch == 0xf4) {
				if (position >= length - 4) {
					bytes = 0;
					return false;
				}

				if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0x8f ||
				    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
				    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf) {
					bytes = 0;
					return false;
				}

				bytes = 4;
				return true;
			}

			if (ch >= 0xf1 && ch <= 0xf3) {
				if (position >= length - 4) {
					bytes = 0;
					return false;
				}

				if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf ||
				    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
				    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf) {
					bytes = 0;
					return false;
				}

				bytes = 4;
				return true;
			}

			return false;
		}
	}
}