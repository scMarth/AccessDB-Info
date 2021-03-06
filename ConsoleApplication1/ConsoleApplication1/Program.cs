﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data;
using System.IO;
using System.Collections;

namespace ConsoleApplication1
{
    class Program
    {
        // Add the string to the hash table. The hash table consists of keys (column) which are strings, and values which are arrays of strings
        // the array contains all the possible values found for a given key string
        private static void addValueToHash(Hashtable hash, string key, string value)
        {
            if (hash == null) return;

            if (hash.Contains(key)) // The hash table contains the key
            {
                List<DictionaryEntry> hashVal = (List<DictionaryEntry>)hash[key]; // Get the list associated with the key

                for (int i=0; i<hashVal.Count(); i++)
                {
                    DictionaryEntry de = hashVal[i];
                    if (de.Key.ToString() == value)
                    {
                        // We found a key/value pair that is the same as a key/value pair we found previously,
                        // so update the frequency count for this key/value pair
                        de.Value = (int)de.Value + 1;
                        hashVal[i] = de;
                        return;
                    }
                }

                // If we got here, the value wasn't in the key array, so create a new entry for it
                DictionaryEntry denew = new DictionaryEntry();
                denew.Key = value;
                int count = 1;
                denew.Value = count;
                hashVal.Add(denew);
                return;

            }else // The hash table doesn't contain this key yet
            {
                List<DictionaryEntry> newHashVal = new List<DictionaryEntry>(); // Create a new list to add to the hash
                DictionaryEntry de = new DictionaryEntry();
                de.Key = value;
                int count = 1;
                de.Value = count;

                newHashVal.Add(de); // Add the value to the list
                hash.Add(key, newHashVal); // Add the list to the hash table
            }
        }

        // Print the contents of the hash table
        private static void dumpHashTable(Hashtable hash, string outputfile)
        {
            Console.WriteLine("Beginning dumpHashTable.");
            foreach (DictionaryEntry de in hash)
            {
                bool skip = false;
                string[] skipList = { "STREET", "ID", "DATE REPORTED", "TEMP REPAIR DATE", "ADDRESS #", "PERM REPAIR DATE"};
                for (int i=0; i<skipList.Length; i++) if (de.Key.ToString() == skipList[i]) skip = true;

                if (skip) continue;

                appendToFile(outputfile, "Key: " + de.Key.ToString() + Environment.NewLine);
                List<DictionaryEntry> list = (List<DictionaryEntry>)de.Value;

                for (int i=0; i<list.Count(); i++)
                {
                    DictionaryEntry ival = list[i];
                    appendToFile(outputfile, "\t" + ival.Key.ToString() + " ; FREQ: " + ival.Value.ToString() + Environment.NewLine);
                }
                appendToFile(outputfile, Environment.NewLine);
            }
            Console.WriteLine("Completed dumpHashTable.");
        }

        // Append string 'str' to output CSV file 'filename'
        private static void appendToFile(string filename, string str)
        {
            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(filename, true))
            {
                file.Write(str);
                file.Flush();
                file.Close();
            }
        }

        private static void createFile(string outfile)
        {
            System.IO.File.WriteAllText(outfile, "");
        }

        private static void getPathsFromConfig(string configFile, ref string basePath, ref string accessDBPath)
        {
            try
            {
                using (StreamReader sr = new StreamReader(configFile))
                {
                    string line1 = sr.ReadLine();
                    string[] tokens1 = line1.Split(' '); //split the line into tokens
                    string line2 = sr.ReadLine();
                    string[] tokens2 = line2.Split(' '); //split the line into tokens

                    basePath = tokens1[1]; // base path is the second token in the first line
                    accessDBPath = basePath + tokens2[1]; // access db path is the second token in the second line

                    return;
                }
            }catch(Exception e)
            {
                Console.WriteLine("getPathsFromConfig failed: {0}", e.ToString());
            }
        }

        static void Main(string[] args)
        {
            string basePath = "";
            string accessDBPath = "";
            string configFilePath = @"./config.txt";

            getPathsFromConfig(configFilePath, ref basePath, ref accessDBPath);

            string outputDBPath = basePath + "OUTPUT.accdb";
            string outputXMLPath = basePath + @"test.xml";
            string outfile1 = basePath + @"tableDump.txt";
            string outfile2 = basePath + @"hashDump.txt";

            Console.WriteLine("basePath: " + basePath);
            Console.WriteLine("accessDBPath: " + accessDBPath);
            Console.WriteLine("outputDBPath: " + outputDBPath);
            Console.WriteLine("outputXMLPath: " + outputXMLPath);
            Console.WriteLine("outfile1: " + outfile1);
            Console.WriteLine("outfile2: " + outfile2);

            // Create a detabase that is a copy of the original to be used for updates,
            // that way the original remains unmodified. Overwrite if it already exists
            System.IO.File.Copy(accessDBPath, outputDBPath, true);

            OleDbConnection connection = new OleDbConnection();

            connection.ConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;"
                + @"Data Source=" + outputDBPath + ";";

            try
            {
                connection.Open();
                Console.WriteLine("Connection opened.");
                DataSet ds = new DataSet();
                Console.WriteLine("Temporary Dataset created.");
                // (only DB table name is SIDEWALK & GUTTER REPAIR LIST)
                OleDbCommand cmd = new OleDbCommand("select * from [SIDEWALK & GUTTER REPAIR LIST]", connection);
                Console.WriteLine("SQL Query set.");
                OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                Console.WriteLine("SQL Query passed.");


                string query = "";
                OleDbCommand cmdUpdate;
                Console.WriteLine(Environment.NewLine + "Attempting to patch typos in database..." + Environment.NewLine);

                // Fix [REPORTED BY]
                query = "update [SIDEWALK & GUTTER REPAIR LIST] set [REPORTED BY]=@rb1 where [REPORTED BY]=@rb2 or [REPORTED BY]=@rb3 or [REPORTED BY]=@rb4";
                query += " or [REPORTED BY]=@rb5 or [REPORTED BY]=@rb6";
                cmdUpdate = new OleDbCommand(query, connection);
                cmdUpdate.Parameters.Clear();
                cmdUpdate.CommandType = CommandType.Text;
                cmdUpdate.Parameters.AddWithValue("rb1", "CITIZEN");
                cmdUpdate.Parameters.AddWithValue("rb2", "CITIZIN");
                cmdUpdate.Parameters.AddWithValue("rb3", ".CITIZEN");
                cmdUpdate.Parameters.AddWithValue("rb4", "11/23/09"); //?
                cmdUpdate.Parameters.AddWithValue("rb5", "CITICEN");
                cmdUpdate.Parameters.AddWithValue("rb6", "CITEZEN");
                Console.WriteLine("Executing SQL update for [REPORTED BY] ...");
                Console.WriteLine("SQL executed. {0} lines affected.", cmdUpdate.ExecuteNonQuery());
                Console.WriteLine();

                // Fix [S/W]
                query = "update [SIDEWALK & GUTTER REPAIR LIST] set [S/W]=@rb1 where StrComp([S/W],@rb2,0) = 0"; // Use StrComp for case sensitivity
                query += "or StrComp([S/W],@rb3,0) = 0 or StrComp([S/W],@rb4,0) = 0";
                query += " or [S/W]=@rb5";
                cmdUpdate = new OleDbCommand(query, connection);
                cmdUpdate.Parameters.Clear();
                cmdUpdate.CommandType = CommandType.Text;
                cmdUpdate.Parameters.AddWithValue("rb1", "X");
                cmdUpdate.Parameters.AddWithValue("rb2", "x");
                cmdUpdate.Parameters.AddWithValue("rb3", "XX");
                cmdUpdate.Parameters.AddWithValue("rb4", "  X");
                cmdUpdate.Parameters.AddWithValue("rb5", "S");
                Console.WriteLine("Executing SQL update for [S/W] ...");
                Console.WriteLine("SQL executed. {0} lines affected.", cmdUpdate.ExecuteNonQuery());
                Console.WriteLine();

                // Fix [C/G]
                query = "update [SIDEWALK & GUTTER REPAIR LIST] set [C/G]=@rb1 where StrComp([C/G],@rb2,0) = 0"; // Use StrComp for case sensitivity
                query += "or StrComp([C/G],@rb3,0) = 0";
                cmdUpdate = new OleDbCommand(query, connection);
                cmdUpdate.Parameters.Clear();
                cmdUpdate.CommandType = CommandType.Text;
                cmdUpdate.Parameters.AddWithValue("rb1", "X");
                cmdUpdate.Parameters.AddWithValue("rb2", "x");
                cmdUpdate.Parameters.AddWithValue("rb3", "5");
                Console.WriteLine("Executing SQL update for [C/G] ...");
                Console.WriteLine("SQL executed. {0} lines affected.", cmdUpdate.ExecuteNonQuery());
                Console.WriteLine();

                // Close the connection
                connection.Close();

                da.Fill(ds);
                Console.WriteLine("Dataset populated.");

                ds.WriteXml(outputXMLPath);

                createFile(outfile1);
                createFile(outfile2);

                Console.WriteLine("Constructing hash and dumping table...");

                // Constructor for a hash table
                Hashtable hash = new Hashtable();

                foreach (DataTable table in ds.Tables)
                {
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        DataRow row = table.Rows[i];
                        
                        for (int j=0; j < table.Columns.Count; j++)
                        {
                            addValueToHash(hash, table.Columns[j].ColumnName, row[j].ToString());
                            //appendToFile(outfile1, table.Columns[j].ColumnName + " : " + row[j].ToString());
                            //appendToFile(outfile1, Environment.NewLine);
                        }
                        //appendToFile(outfile1, Environment.NewLine);
                    }
                }

                dumpHashTable(hash, outfile2);
                Console.WriteLine("Successfully completed processing. Press any key to exit.");
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to connect to data source.");
            }
            finally
            {
                connection.Close();
            }
            Console.ReadKey();
        }
    }
}
