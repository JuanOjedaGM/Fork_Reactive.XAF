﻿using System.Text;

namespace Xpand.Extensions.BytesExtensions{
	public static partial class BytesExtensions{
		public static string GetString(this byte[] bytes, Encoding encoding = null){
			if (bytes == null) {
				return null;
			}
			encoding ??= Encoding.UTF8;
			return encoding.GetString(bytes);
		}
	}
}