using System;
using System.Runtime.Serialization;
using CimCareMod.AI;

namespace CimCareMod.Utils
{
    public class BackwardСompatibilityBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            if (assemblyName == "CimCareMod")
            {
                switch (typeName)
                {
                    case "SeniorCitizenCenterMod.NursingHomeAi": return typeof(NursingHomeAI);
                }
            }

            var type = Type.GetType($"{typeName}, {assemblyName}");
            return type;
        }
    }
}