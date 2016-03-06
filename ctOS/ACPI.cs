using System;
using Cosmos.Core;

namespace ctOS
{
	public unsafe class ACPI
	{
		#region Variables

		private static int* _smiCmd;
		private static byte _acpiEnable;
		private static byte _acpiDisable;
		private static int* _pm1ACnt;
		private static int* _pm1BCnt;
		private static short _slpTyPa;
		private static short _slpTyPb;
		private static short _slpEn;
		//private static short _sciEn;
		//private static byte _pm1CntLen;

		static byte* _facp = null;

		private static IOPort _smiIo, _pm1AIo, _pm1BIo;

		#endregion

		#region Util Methods

		static int Compare(string c1, byte* c2)
		{
			for (var i = 0; i < c1.Length; i++)
			{
				if (c1[i] != (char)c2[i]) { return -1; }
			}
			return 0;
		} 

		#endregion

		#region Structs

/*
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct RSDPtr
		{
			private fixed byte Signature[8];
			private readonly byte CheckSum;
			private fixed byte OemID[6];
			private readonly byte Revision;
			public readonly int RsdtAddress;
		};
*/

/*
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct FACP
		{
			public fixed byte Signature[4];
			public int Length;
			public fixed byte unneded1[40 - 8];
			public int* DSDT;
			public fixed byte unneded2[48 - 44];
			public int* SMI_CMD;
			public byte ACPI_ENABLE;
			public byte ACPI_DISABLE;
			public fixed byte unneded3[64 - 54];
			public int* PM1a_CNT_BLK;
			public int* PM1b_CNT_BLK;
			public fixed byte unneded4[89 - 72];
			public byte PM1_CNT_LEN;
		}; 
*/

		#endregion

		#region Gets

		static int* FACPGet(int number)
		{
			switch (number)
			{
				case 0:
					return (int*)*((int*)(_facp + 40));
				case 1:
					return (int*)*((int*)(_facp + 48));
				case 2:
					return (int*)*((int*)(_facp + 64));
				case 3:
					return (int*)*((int*)(_facp + 68));
				default:
					return null;
			}
		}

		static byte FACPBGet(int number)
		{
			switch (number)
			{
				case 0:
					return *(_facp + 52);
				case 1:
					return *(_facp + 53);
				case 2:
					return *(_facp + 89);
				default:
					return 0;
			}
		}

		static uint RSDPAddress()
		{

			// check bios
			for (uint addr = 0xE0000; addr < 0x100000; addr += 4)
				if (Compare("RSD PTR ", (byte*)addr) == 0)
					if (Check_RSD(addr))
						return addr;
			// check extended bios
			uint ebdaAddress = *((uint*)0x040E);

			ebdaAddress = (ebdaAddress * 0x10) & 0x000fffff;

			for (uint addr = ebdaAddress; addr < ebdaAddress + 1024; addr += 4)
				if (Compare("RSD PTR ", (byte*)addr) == 0)
					return addr;

			// not found
			return 0;
		} 

		#endregion

		#region Checks

/*
		/// <summary>
		/// Checks if the given address has a valid header
		/// </summary>
		static uint* acpiCheckRSDPtr(uint* ptr)
		{
			string sig = "RSD PTR ";
			RSDPtr* rsdp = (RSDPtr*)ptr;
			byte* bptr;
			byte check = 0;
			int i;

			if (Compare(sig, (byte*)rsdp) == 0)
			{
				// check checksum rsdpd
				bptr = (byte*)ptr;
				for (i = 0; i < 20; i++)
				{
					check += *bptr;
					bptr++;
				}
				// found valid rsdpd   
				if (check == 0)
				{
					Compare("RSDT", (byte*)rsdp->RsdtAddress);
					if (rsdp->RsdtAddress != 0)
						return (uint*)rsdp->RsdtAddress;
				}
			}
			Console.WriteLine("Unable to find RSDT. ACPI not available.");
			return null;
		}
*/

		/// <summary>
		/// Checks for a given header and validates checksum
		/// </summary>
		static int ACPICheckHeader(byte* ptr, string sig)
		{
			return Compare(sig, ptr);
		}

		static bool Check_RSD(uint address)
		{
			byte sum = 0;
			var check = (byte*)address;

			for (var i = 0; i < 20; i++)
				sum += *(check++);

			return (sum == 0);
		} 

		#endregion

		#region Public Methods

		/// <summary>
		/// Check if ACPI is not already enabled, and if it can be, enable it.
		/// </summary>
		/// <returns>Boolean indication of success.</returns>
		public static bool Enable()
		{
			// Check if ACPI is enabled
			if (_pm1AIo.Word == 0)
			{
				// Check if ACPI can be enabled
				if (_smiCmd != null && _acpiEnable != 0)
				{
					_smiIo.Byte = _acpiEnable;
					// Give 3 seconds time to enable ACPI

					int i;
					for (i = 0; i < 300; i++)
						if ((_pm1AIo.Word & 1) == 1)
							break;

					if (_pm1BCnt != null)
						for (; i < 300; i++)
							if ((_pm1BIo.Word & 1) == 1)
								break;
					if (i < 300)
						return true;
					Console.WriteLine("Could not enable ACPI.");
					return false;
				}
				Console.WriteLine("No known way to enable ACPI.");
				return false;
			}
			return true;
		}

		/// <summary>
		/// Disables ACPI
		/// </summary>
		public static void Disable()
		{
			_smiIo.Byte = _acpiDisable;
		}

		/// <summary>
		/// Initializes the ACPI Interface
		/// </summary>
		/// <returns>Boolean indication of success</returns>
		public static bool Init()
		{
			var ptr = (byte*)RSDPAddress(); int addr = 0;

			for (int i = 19; i >= 16; i--)
			{
				addr += (*(ptr + i));
				addr = (i == 16) ? addr : addr << 8;
			}

			ptr = (byte*)addr;
			ptr += 4; addr = 0;
			for (int i = 3; i >= 0; i--)
			{
				addr += (*(ptr + i));
				addr = (i == 0) ? addr : addr << 8;
			}
			int length = addr;

			ptr -= 4;
			// check if address is correct  ( if acpi is available on this pc )
			if (ptr != null && ACPICheckHeader(ptr, "RSDT") == 0)
			{
				addr = 0;
				// the RSDT contains an unknown number of pointers to acpi tables

				var entrys = length;
				entrys = (entrys - 36) / 4;
				ptr += 36;   // skip header information

				while (0 < entrys--)
				{
					for (var i = 3; i >= 0; i--)
					{
						addr += (*(ptr + i));
						addr = (i == 0) ? addr : addr << 8;
					}
					var yeuse = (byte*)addr;
					// check if the desired table is reached
					_facp = yeuse;
					if (Compare("FACP", _facp) == 0)
					{
						if (ACPICheckHeader((byte*)FACPGet(0), "DSDT") == 0)
						{
							// search the \_S5 package in the DSDT
							byte* s5Addr = (byte*)FACPGet(0) + 36; // skip header
							int dsdtLength = *(FACPGet(0) + 1) - 36;
							while (0 < dsdtLength--)
							{
								if (Compare("_S5_", s5Addr) == 0)
									break;
								s5Addr++;
							}
							// check if \_S5 was found
							if (dsdtLength > 0)
							{
								// check for valid AML structure
								if ((*(s5Addr - 1) == 0x08 || (*(s5Addr - 2) == 0x08 && *(s5Addr - 1) == '\\')) && *(s5Addr + 4) == 0x12)
								{
									s5Addr += 5;
									s5Addr += ((*s5Addr & 0xC0) >> 6) + 2;   // calculate PkgLength size

									if (*s5Addr == 0x0A)
										s5Addr++;   // skip byte prefix
									_slpTyPa = (short)(*(s5Addr) << 10);
									s5Addr++;

									if (*s5Addr == 0x0A)
										s5Addr++;   // skip byte prefix
									_slpTyPb = (short)(*(s5Addr) << 10);

									_smiCmd = FACPGet(1);

									_acpiEnable = FACPBGet(0);
									_acpiDisable = FACPBGet(1);

									_pm1ACnt = FACPGet(2);
									_pm1BCnt = FACPGet(3);

									//_pm1CntLen = FACPBGet(3);

									_slpEn = 1 << 13;
									//_sciEn = 1;
									_smiIo = new IOPort((ushort)_smiCmd);
									_pm1AIo = new IOPort((ushort)_pm1ACnt);
									_pm1BIo = new IOPort((ushort)_pm1BCnt);
									return true;
								}
								Console.WriteLine("\\_S5 parse error.");
							}
							else
							{
								Console.WriteLine("\\_S5 not present.");
							}
						}
						else
						{
							Console.WriteLine("DSDT is invalid.");
						}
					}
					ptr += 4;
				}
				Console.WriteLine("No valid FACP present.");
			}
			else
			{
				Console.WriteLine("No ACPI interface available...");
			}

			return false;
		}

		/// <summary>
		/// Attempts to shut down the computer.
		/// </summary>
		public static void Shutdown()
		{
			// Send the shutdown command
			Console.Clear();
			if (_pm1ACnt == null) Init();
			if (_pm1AIo != null)
			{
				_pm1AIo.Word = (ushort)(_slpTyPa | _slpEn);
				if (_pm1BCnt != null)
					_pm1BIo.Word = (ushort)(_slpTyPb | _slpEn);
			}
			Console.Write("It is now safe to turn off your computer");
		} 

		#endregion
	}
}