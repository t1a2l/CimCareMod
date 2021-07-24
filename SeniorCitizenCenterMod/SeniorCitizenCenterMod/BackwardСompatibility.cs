using System;
using System.Runtime.Serialization;

namespace SeniorCitizenCenterMod
{
    public class BackwardСompatibilityBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            if (assemblyName == "SeniorCitizenCenterMod")
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