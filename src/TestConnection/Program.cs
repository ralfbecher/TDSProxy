using System;
using Microsoft.Data.SqlClient;

namespace TestConnection
{
	static class Program
	{
		static void Main()
		{
			// Use SqlConnection for cross-platform compatibility
			var cn = new SqlConnection("Server=pc-mattw.techsoftwareinc.com;Database=IRBM_Dev;User Id=dev\\mattw;Password=dev;MultipleActiveResultSets=false;TrustServerCertificate=true");
			cn.Open();
			while (true)
			{
				var cmd = cn.CreateCommand();
				cmd.CommandText = "SELECT * FROM Note";
				var dr = cmd.ExecuteReader();
				while (dr.Read())
				{
					for (int i = 0; i < dr.FieldCount; i++)
						Console.Write("{0}\t", dr.GetValue(i));
					Console.WriteLine();
				}

				if (Console.ReadKey(false).Key == ConsoleKey.Escape)
					break;
			}
		}
	}
}
