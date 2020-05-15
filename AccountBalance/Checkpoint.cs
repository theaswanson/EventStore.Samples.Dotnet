using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountBalance
{
    public class Checkpoint
    {
        public int? ID { get; private set; }
        public int Value { get; private set; }
        string _file;
        public Checkpoint(string file)
        {
            _file = file;
            Load();
        }

        void Load()
        {
            if (!File.Exists(_file))
            {
                Reset();
            }

            try
            {
                var text = File.ReadAllText(_file);
                var tokens = text.Split(',');
                if (tokens.Length == 2)
                {
                    ID = int.Parse(tokens[0]);
                    Value = int.Parse(tokens[1]);
                }
            }
            catch
            {
                Reset();
            }
        }

        public void Save(int checkpointId, int value)
        {
            File.WriteAllText(_file, checkpointId + "," + value);
            Value = value;
            ID = checkpointId;
        }

        private void Reset()
        {
            ID = null;
            Value = 0;
        }
    }
}
