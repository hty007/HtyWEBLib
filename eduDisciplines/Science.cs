﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using htyWEBlib.data;

namespace htyWEBlib.eduDisciplines
{
    public class Science: IHData, IComparable, IEnumerable<Science>, IHStringData
    {
        #region Поля и свойства
        /// <summary>Название </summary>
        public string Name { get; set; }
        /// <summary>Индивидуальный порядковый номер</summary>
        public int ID { get; set; }
        /// <summary>Тип отрывка</summary>
        public ScienceType Type { get; set; }
        public DistributionType Distribution { get; set; }
        protected List<Science> content;
        protected Science master;        
        public Science this[int index] => content.Find(n => n.ID == index);
        /// <summary>Количество состовных частей</summary>
        public int Count { get => content.Count; }
        /// <summary>Получить на уровень выше</summary>
        public Science GetMaster() => master;
        #endregion
        #region Конструкторы
        public Science()
        {
            content = new List<Science>();
            Distribution = new DistributionType();
        }
        public Science(string name, int id, ScienceType type = ScienceType.theme):this()
        {
            master = null;
            Name = name;
            ID = id;
            Type = type;
        }
        #endregion
        #region Функционал
        /// <summary>
        /// Получить код 
        /// </summary>
        /// <param name="premier">Сколько уровень срезать</param>
        /// <returns>Строка с кодом</returns>
        public string GetCode(int premier = -1)
        {
            if (premier == -1)
                return "";
            List<int> ids = new List<int>
            {
                ID
            };
            var sc = master;
            while (sc != null)
            {
                ids.Add(sc.ID);
                sc = sc.master;
            }
            ids.Reverse();            
            ids.RemoveRange(0, premier);
            string code = string.Join(".",ids.ToArray());
            return code;
        }
        public bool ContainIn(int id)
        {
            return null != content.Find(n => n.ID == id);
        }
        /// <summary>
        /// Найти нужную тему 
        /// </summary>
        /// <returns>The science.</returns>
        /// <param name="code">Нет проверки на null</param>
        public Science GetScience(string code)
        {
            if (GetCode(0) == code)
                return this;
            Science result = null;
            foreach (Science s in content)
            {                
                result = s.GetScience(code);
                if (result != null)
                    break;
            }
            return result;
            
            //throw new NotImplementedException();
        }
        public void Delete(int id)
        {            
            for (int i = 0; i < Count; i++)
            {
                if (content[i].ID == id)
                    content.RemoveAt(i);
            }
        }
        public void SetMaster(Science master)
        {
            this.master = master;            
        }
        public void ReIndex()
        {
            for (int i = 0; i < Count; i++)
            {
                content[i].ID = i+1;
            }
        }
        public virtual void Add(Science science)
        {
            if (content.Find(n => n.ID == science.ID)!= null)
                throw new ArgumentException();
            content.Add(science);
            science.master = this;
            content.Sort();            
        }
        public void Add(string name, int id, ScienceType type = ScienceType.theme)
        {
            Science section = new Science(name, id, type);            
            Add(section);
        }
        #endregion
        #region Переопределения Интерфейсы и файлы
        public int CompareTo(object obj)
        {
            Science sc = (Science)obj;
            return ID.CompareTo(sc.ID);
        }
        public override string ToString() => string.Format("{0}", GetCode(0));
        public IEnumerator<Science> GetEnumerator() => ((IEnumerable<Science>)content).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<Science>)content).GetEnumerator();
        private const string Separetor = "#!_!#";
        public void Load(StreamReader sr)
        {
            var t = sr.ReadLine();
            var item = t.Split(new [] { Separetor}, StringSplitOptions.RemoveEmptyEntries);
            ID = int.Parse(item[0]);
            Name = item[1];
            Type = item[2].ToScienceType();
            int count = int.Parse(item[3]);
            Distribution.Decoder(item[4]);
            for (int i = 0; i < count; i++)
            {
                Science sc = new Science();
                sc.Load(sr);
                //var s = (IHStringData)sc;
                //s.Load(sr);
                Add(sc);
            }

            //throw new NotImplementedException();
        }
        public void Save(StreamWriter sw)
        {
            string result = string.Join(Separetor, ID ,  Name, Type, Count, Distribution.Coder());
            sw.WriteLine(result);
            foreach (IHStringData sc in content)
            {
                sc.Save(sw);
            }

            //throw new NotImplementedException();
        }
        public void Load(BinaryReader reader)
        {
            var data = new Pairs(reader);
            int count = 0;
            foreach (var p in data)
            {
                switch (p.Key)
                {
                    case "t": Type = (ScienceType)p.ValueInt; break;
                    case "id": ID = p.ValueInt; break;
                    case "n": Name = p.ValueString; break;
                    case "d": Distribution.InPairs((Pairs)p.Value); break;
                    case "cc": count = p.ValueInt; break;
                }
            }                      
            for (int i = 0; i < count; i++)
            {
                Science item = new Science();
                item.Load(reader);
                Add(item);
            }
        }
        public void Save(BinaryWriter writer)
        {
            var data = new Pairs();
            data.Add("t", (int)Type);
            data.Add("id", ID);
            data.Add("n", Name);
            data.Add("d", Distribution);
            data.Add("cc", content.Count);
            data.Save(writer);
            foreach (var item in content)
            {
                item.Save(writer);
            }
        }
        public void LoadOld(BinaryReader reader)
        {
            Type = (ScienceType)reader.ReadInt32();            
            ID = reader.ReadInt32();
            Name = reader.ReadString();
            Distribution.Load(reader);
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Science item = new Science();
                item.LoadOld(reader);
                Add(item);
            }
        }
        public void SaveOld(BinaryWriter writer)
        {
            writer.Write((int)Type);            
            writer.Write(ID);
            writer.Write(Name);
            Distribution.Save(writer);                    
            writer.Write(content.Count);
            foreach (var item in content)
            {
                item.SaveOld(writer);
            }
        }
        #endregion
    }
    public class DistributionType : IHData
    {
        public const int Count = 7;
        private bool[] data;            
        public bool this[int index] {
            get {
                if (index < 7 || index > 11)
                    return false;
                return data[index - 7];
            }
            set
            {
                if (index >= 7 && index <= 11)
                    data[index - 7] = value;
            } }
        public bool OGE9 { get => data[5]; set => data[5] = value; }
        public bool OGE11 { get => data[6]; set => data[6] = value; }
        public DistributionType()
        {
            data = new bool[Count];
        }
        public void Load(BinaryReader reader)
        {
            var dat = new Pairs(reader);
            InPairs(dat);
        }
        public void InPairs(Pairs dat)
        {
            foreach (var p in dat)
            {
                if (p.ValueBool)
                {
                    int n = int.Parse(p.Key);
                    data[n] = true;
                }
            }
        }
        public void Save(BinaryWriter writer)
        {
            var dat = new Pairs();
            for (int i = 0; i < Count; i++)
            {
                if (data[i])
                    dat.Add(i.ToString(), data[i]);                 
            }
            dat.Save(writer);
        }
        public string Coder()
        {
            char[] d = new char[Count];
            for (int i = 0; i < Count; i++)
            {
                d[i] = data[i] ? 't' : 'f';
            }
            return string.Concat(d);
        }
        public void Decoder(string code)
        {
            char[] d = code.ToCharArray();
            for (int i = 0; i < Count; i++)
            {
                data[i] = d[i] == 't';
            }
        }
        public override string ToString()
        {
            StringBuilder res = new StringBuilder();
            for (int i = 7; i < 12; i++)
                if (this[i])
                {
                    if (res.Length != 0) res.Append(", ");
                    res.Append(i.ToString());
                }                
                
            if (OGE9)
            {
                if (res.Length != 0) res.Append(", ");
                res.Append("ОГЭ9");
            }                
            if (OGE11)
            {
                if (res.Length != 0) res.Append(", ");
                res.Append("ОГЭ11");
            }
            return res.ToString();
        }

       
    }
    public enum ScienceType
    {
        theme,
        text,
        formyle,
        subtheme,
        section,
        definition
    }
    public static class ScienceTypeHelper
    {
        public static string ToRus(this ScienceType type)
        {
            switch (type)
            {
                case ScienceType.formyle:return "Формула";
                case ScienceType.text: return "Текст"; 
                case ScienceType.theme: return "Тема";
                case ScienceType.subtheme: return "Подтема";
                case ScienceType.section: return "Раздел";
                case ScienceType.definition: return "Определение";

                default:
                    throw new ArgumentException($"Не предусмотрено: {type}.");
            }
        }
        public static ScienceType ToScienceType(this string text)
        {
            return (ScienceType)Enum.Parse(typeof(ScienceType), text);
            
        }
        
    }

}
