using System;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Windows.Forms;
using System.Threading;


namespace USBDeleter
{
	public class RegWork
	{
		/// <summary>
		/// Path to search devices
		/// </summary>
		private string _mainKeyReg = "";
		/// <summary>
		/// selected serial number of device
		/// </summary>
		private static string _serial_number;
		/// <summary>
		/// Main registry key
		/// </summary>
		private RegistryKey _rootKey = null;
		/// <summary>
		/// Main registry keyS
		/// </summary>
		private List<RegistryKey> _registryHives = new List<RegistryKey>
		{
			Registry.CurrentUser,
			Registry.LocalMachine,
			Registry.ClassesRoot,
			Registry.CurrentConfig,
			Registry.Users,
			Registry.PerformanceData
		};
		/// <summary>
		/// Control in which we write current registry path
		/// </summary>
		private Control _outputControl = null;
		private bool _serchUSBStr;
		private bool _searchMobileStr;
		/// <summary>
		/// Dictionary that include ( registry path [key,value] )
		/// </summary>
		public Dictionary<string, Dictionary<string, string>> pathContent;
		public bool pathDeleted = false;
		public bool keyValDeleted = false;
		/// <summary>
		/// Create copy of RegWork
		/// </summary>
		/// <param name="rootKey">Registry root key [HKCR,HKCU,HKLM,HKU,HKCC] </param>
		/// <param name="outputControl">Control that have Text-property</param>
		public RegWork(RegistryKey rootKey, Control outputControl = null)
		{
			pathContent = new Dictionary<string, Dictionary<string, string>>();
			_serial_number = "-1";
			if (rootKey != null) _rootKey = rootKey;
			_outputControl = outputControl;
		}

		/// <summary>
		/// Serching serial number of device ih Registry-path
		/// </summary>
		/// <param name="key">Registry-path</param>
		private void SearchInLocation(RegistryKey key)
        {
			if (key == null) return;

			if (key.Name.Contains(_serial_number))//search in Location
			{
				pathContent.Add(key.Name, new Dictionary<string, string>());
			}
		}
		/// <summary>
		/// Serching serial number of device ih Registry-path that have Key/Value
		/// </summary>
		/// <param name="key">Registry-path</param>
		private void SearchInKeysValues(RegistryKey key)
		{
			if (key == null) return;

			if (key.hasKeyNames())//search in Keys & Values
			{
				foreach (var k in key.GetValueNames())
				{
					if (k.Contains(_serial_number) || key.GetValue(k, "").ToString().Contains(_serial_number))
					{
						if (pathContent.ContainsKey(key.Name))
						{
							pathContent[key.Name].Add(k, key.GetValue(k).ToString()); 
						}
						else
						{
							pathContent.Add(key.Name, new Dictionary<string, string>());
							pathContent[key.Name]?.Add(k, key.GetValue(k).ToString());
						}
					}
				}
			}
		}
		/// <summary>
		/// Collecting all devices
		/// </summary>
		/// <returns>string[] of devices </returns>
		public string[] GetAllUsbDevices(bool usbstrORmobile = true)
		{
            if (!usbstrORmobile)
            {
				_rootKey = Registry.LocalMachine;
				_searchMobileStr = false;
				_serchUSBStr = true;
				_mainKeyReg = @"SYSTEM\CurrentControlSet\Enum\USBSTOR";//@"SYSTEM\Setup\Upgrade\PnP\CurrentControlSet\Control\DeviceMigration\Devices\USBSTOR";
			} 
			else
            {
				_rootKey = Registry.CurrentUser;
				_searchMobileStr = true;
				_serchUSBStr = false;
				_mainKeyReg = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\AutoplayHandlers\KnownDevices";
			}

			RegistryKey usbStor = _rootKey.OpenSubKey(_mainKeyReg,false);
			string[] collectionUsb = usbStor.GetSubKeyNames();
			usbStor.Close();
			return collectionUsb;
		}
		/// <summary>
		/// Collecting serial number of current device
		/// </summary>
		/// <param name="usbStor">Name of device</param>
		/// <returns>string[] of serial number devices</returns>
		public string[] GetSerialOfCurrentUsb(string usbStor)
        {
			RegistryKey currentUsb = _rootKey.OpenSubKey($"{_mainKeyReg}\\{usbStor}");
			string[] collectionOfCurrentUsbSerialNums = new string[0];

			if (_serchUSBStr)
				collectionOfCurrentUsbSerialNums = currentUsb.GetSubKeyNames();

            if (_searchMobileStr)
            {
				Array.Resize(ref collectionOfCurrentUsbSerialNums, collectionOfCurrentUsbSerialNums.Length + 1);
				collectionOfCurrentUsbSerialNums.SetValue(currentUsb.GetValue("Label").ToString(), collectionOfCurrentUsbSerialNums.Length-1);
			}

			currentUsb.Close();
			return collectionOfCurrentUsbSerialNums;
		}

		/// <summary>
		/// Search by all HK
		/// </summary>
		/// <param name="token">Token that can Cancel search</param>
		/// <param name="sernum">Selected serial number/name of device</param>
		public void Find(CancellationToken token, string sernum = "")
        {
			if (!string.IsNullOrEmpty(sernum))
				_serial_number = sernum;

			for (int i = 0; i < _registryHives.Count - 1; i++)
			{
				_rootKey = _registryHives[i];
				FindAllEntrysOfCurrentUsbSN(token, _rootKey);
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();
            }
        }


		/// <summary>
		/// Search all atempt of serial number in Registry root key
		/// </summary>
		/// <param name="token">Token that can Cancel search</param>
		/// <param name="ck">Registry root/current key</param>
		public void FindAllEntrysOfCurrentUsbSN(CancellationToken token, RegistryKey ck = null)
        {
			foreach (var item in ck.GetSubKeyNames())
			{
				if (token.IsCancellationRequested)
					break;

				//full path to last inner folder
				string path = ck.Name.Contains("\\") ? 
							$"{ck.Name.Replace(_rootKey.Name+ "\\", "")}\\{item}" 
							: ck.Name.Replace(_rootKey.Name, item);

                if (_outputControl!=null)
                {
					Action act = () => { _outputControl.Text = path; };
					_outputControl.Invoke(act);
				}

				try
                {
					RegistryKey k = _rootKey.OpenSubKey(path, false);
					SearchInLocation(k);
					SearchInKeysValues(k);

					if (k.hasCildren())
						FindAllEntrysOfCurrentUsbSN(token, k );

					k?.Close();
				}
				catch (Exception ex)
				{
					// show/log message about access denided OR ignor attempt
					//throw;
				}
				
			}

			//if (token.IsCancellationRequested)
			//	token.ThrowIfCancellationRequested();
		}


		public void DeleteNotUsedKey(string key)
        {
			this._registryHives.RemoveAll(x => x.Name == key);
		}



		/// <summary>
		/// Deleting current Registry-path
		/// </summary>
		/// <param name="path">Selected Registry-path</param>
		/// <param name="key">key</param>
		/// <param name="value">value</param>
		/// <param name="del_path">Need to delete main Registry-path of device</param>
		/// <returns>Deleted path</returns>
		public bool DeleteSelectedFolderKeyValue(string path, string key, string value, bool del_path)
        {

			string folderPath = "";
			RegistryKey rk = null;

			path = path.Remove(0, path.IndexOf("\\") + 1).Trim();

			//collecting path to folder that contain SN. If we want to delete all inner folders
			if (path.Contains(_serial_number))
			{
				string[] newP = path.Split('\\');
				
				foreach (var s in newP)
				{
					if (!s.Contains(_serial_number))
						folderPath += s + "\\";
					else
					{
						folderPath += s + "\\";
						break;
					}
				}
			}

            if (del_path) path = folderPath;

			try
			{
				rk = _rootKey.OpenSubKey(path, true);
				
				if (del_path)
				{
					key = string.Empty;
					rk.DeleteSubKeyTree(key);
					pathDeleted = true;
				}
				else
				{
					if (!string.IsNullOrEmpty(key) || !string.IsNullOrEmpty(value))
                    {
						rk.DeleteValue(key);
						keyValDeleted = true;
					}

                    else
                    {
						pathDeleted = true;
						rk.DeleteSubKeyTree(key);
					}
                }

				MessageBox.Show("Deleted!");
				return true;
			}
            catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				return false;
				throw;
			}
			finally 
			{
                if (rk != null)
					rk.Close();
			}
        }
	}
}
