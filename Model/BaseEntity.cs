using System;
using System.Collections.Generic;
using System.Text;

namespace Model
{
    public abstract class BaseEntity
    {
        // תכונה שמשותפת לכל הטבלאות במסד הנתונים
        public int Id { get; set; }

        // בנאי ריק (חובה כשעובדים עם מסדי נתונים)
        protected BaseEntity() { }

        // בנאי שמקבל מזהה
        protected BaseEntity(int id)
        {
            Id = id;
        }
    }
}
