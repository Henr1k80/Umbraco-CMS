﻿using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Umbraco.Web.Models.ContentEditing
{
   
    [DataContract(Name = "contentType", Namespace = "")]
    public class ContentTypeDisplay : ContentTypeCompositionDisplay
    {
        public ContentTypeDisplay()
        {
            //initialize collections so at least their never null
            AllowedTemplates = new List<EntityBasic>();            
        }

        //name, alias, icon, thumb, desc, inherited from the content type

        // Templates
        [DataMember(Name = "allowedTemplates")]
        public IEnumerable<EntityBasic> AllowedTemplates { get; set; } 

        [DataMember(Name = "defaultTemplate")]
        public EntityBasic DefaultTemplate { get; set; }

        [DataMember(Name = "firstContentType")]
        public bool FirstContentType { get; set; }
    }
}
