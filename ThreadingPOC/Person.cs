using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreadingPOC
{
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Sex { get; set; }

        public Person(string name, int age, string sex)
        {
            this.Name = name;
            this.Age = age;
            this.Sex = sex;
        }
    }
}
