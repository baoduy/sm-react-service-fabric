using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Threading.Tasks;

namespace Web
{
    public class ConfigHelper
    {
        private readonly ServiceContext _context;
        private readonly string _name;

        public ConfigHelper(ServiceContext context, string name = "Config")
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        private ConfigurationPackage GetConfigurationPackage()
            => _context.CodePackageActivationContext.GetConfigurationPackageObject(_name);

        public ConfigurationSection GetSection(string sectionName)
            => GetConfigurationPackage().Settings.Sections[sectionName];

        public virtual string GetValue(string sectionName, string name)
            => GetSection(sectionName).Parameters[name].Value;
    }
}
