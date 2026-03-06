using System;
using System.Collections.Generic;

namespace ExampleApi.Model;

public partial class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public virtual ICollection<RoleUser> RoleUsers { get; set; } = new List<RoleUser>();
}
