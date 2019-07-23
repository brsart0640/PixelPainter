﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GUIPixelPainter
{
    public class DataLoader
    {
        private GUIDataExchange dataExchange;
        public DataLoader(GUIDataExchange dataExchange)
        {
            this.dataExchange = dataExchange;
        }

        private string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PixelPainter");
        private string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PixelPainter/config.json");

        public void Load()
        {
            if (!File.Exists(configPath))
                return;

            using (StreamReader file = File.OpenText(configPath))
            {
                JsonSerializer serializer = new JsonSerializer();
                var data = (Tuple<Dictionary<int, List<GUITask>>, List<GUIUser>, bool, int>)serializer.Deserialize(file, typeof(Tuple<Dictionary<int, List<GUITask>>, List<GUIUser>, bool, int>));

                var tasks = data.Item1;
                var users = data.Item2;
                bool overlayTasks = data.Item3;
                int canvasId = data.Item4;

                //load images and push tasks
                foreach (KeyValuePair<int, List<GUITask>> canvas in tasks)
                {
                    foreach (GUITask task in canvas.Value)
                    {
                        Bitmap original, converted, dithered;
                        try
                        {
                            original = LoadBitmap(Path.Combine(folderPath, "original", task.InternalId + ".png"));
                            converted = LoadBitmap(Path.Combine(folderPath, "converted", task.InternalId + ".png"));
                            dithered = LoadBitmap(Path.Combine(folderPath, "dithered", task.InternalId + ".png"));
                        }
                        catch (FileNotFoundException)
                        {
                            return;
                        }
                        GUITask newTask = new GUITask(task.InternalId, task.Name, task.Enabled, task.X, task.Y, task.Dithering, task.KeepRepairing, original, converted, dithered);
                        dataExchange.PushNewTask(newTask, canvas.Key);
                    }
                }

                //push users
                foreach (GUIUser user in users)
                {
                    dataExchange.PushNewUser(user);
                }
                dataExchange.PushSettings(overlayTasks, canvasId);
            }
        }

        private static Bitmap LoadBitmap(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var ms = new MemoryStream(bytes);
            var img = (Bitmap)Image.FromStream(ms);
            return img;
        }

        private void ClearDirectory(string path)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(path);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(folderPath);
            Directory.CreateDirectory(Path.Combine(folderPath, "original"));
            Directory.CreateDirectory(Path.Combine(folderPath, "converted"));
            Directory.CreateDirectory(Path.Combine(folderPath, "dithered"));

            //Delete old images
            ClearDirectory(Path.Combine(folderPath, "original"));
            ClearDirectory(Path.Combine(folderPath, "converted"));
            ClearDirectory(Path.Combine(folderPath, "dithered"));

            using (StreamWriter file = new StreamWriter(File.Create(configPath)))
            {
                JsonSerializer serializer = new JsonSerializer();
                Dictionary<int, List<GUITask>> tasks = dataExchange.GUITasks.Select((a) => a).ToDictionary((a) => a.Key, (a) => a.Value.Select((b) => b).ToList());
                List<GUIUser> users = dataExchange.GUIUsers.Select((a) => a).ToList();
                bool overlayTasks = dataExchange.OverlayTasks;
                int canvasId = dataExchange.CanvasId;
                serializer.Serialize(file, new Tuple<Dictionary<int, List<GUITask>>, List<GUIUser>, bool, int>(tasks, users, overlayTasks, canvasId));

                //save images
                foreach (KeyValuePair<int, List<GUITask>> pair in tasks)
                {
                    foreach (GUITask task in pair.Value)
                    {
                        task.OriginalBitmap.Save(Path.Combine(folderPath, "original", task.InternalId + ".png"));
                        task.ConvertedBitmap.Save(Path.Combine(folderPath, "converted", task.InternalId + ".png"));
                        task.DitheredConvertedBitmap.Save(Path.Combine(folderPath, "dithered", task.InternalId + ".png"));
                    }
                }
            }
        }
    }
}