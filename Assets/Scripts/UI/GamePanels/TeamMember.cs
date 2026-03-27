using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewTeamMember", menuName = "Running Late/Credits/Team Member")]
public class TeamMember : ScriptableObject
{
    [Header("Personal Info")]
    public string displayName;
    
    [Header("Contributor Type")]
    public ContributorType contributorType = ContributorType.CoreTeam;
    
    [Header("Roles (can have multiple)")]
    [Tooltip("Add multiple roles for people wearing multiple hats")]
    public List<Role> roles = new List<Role>();
    
    [Header("Display Settings")]
    public int displayOrder = 0;
    
    [Header("Optional Info")]
    [Tooltip("e.g., @twitter, LinkedIn, portfolio URL")]
    public string socialHandle;
    
    [Tooltip("Additional credit line, e.g., 'Character Sprites', 'Gold Tier Supporter'")]
    [TextArea(2, 3)]
    public string additionalCredit;
    
    public string GetFormattedRoles()
    {
        if (roles == null || roles.Count == 0)
        {
            return "Team Member";
        }
        
        List<string> roleTitles = new List<string>();
        foreach (var role in roles)
        {
            roleTitles.Add(role.GetFormattedTitle());
        }
        
        return string.Join(" • ", roleTitles);
    }
    
    public int GetPrimarySortOrder()
    {
        int typeOrder = (int)contributorType * 100000;
        
        if (roles != null && roles.Count > 0)
        {
            int minRolePriority = int.MaxValue;
            foreach (var role in roles)
            {
                minRolePriority = Mathf.Min(minRolePriority, role.GetSortPriority());
            }
            return typeOrder + minRolePriority + displayOrder;
        }
        
        return typeOrder + displayOrder;
    }
    
    public bool HasDepartment(Department dept)
    {
        if (roles == null) return false;
        
        foreach (var role in roles)
        {
            if (role.department == dept)
            {
                return true;
            }
        }
        
        return false;
    }
    
    public Seniority GetHighestSeniorityInDepartment(Department dept)
    {
        Seniority highest = Seniority.Contributor;
        
        if (roles == null) return highest;
        
        foreach (var role in roles)
        {
            if (role.department == dept && role.seniority < highest)
            {
                highest = role.seniority;
            }
        }
        
        return highest;
    }
    
    public List<Role> GetRolesInDepartment(Department dept)
    {
        List<Role> deptRoles = new List<Role>();
        
        if (roles == null) return deptRoles;
        
        foreach (var role in roles)
        {
            if (role.department == dept)
            {
                deptRoles.Add(role);
            }
        }
        
        return deptRoles;
    }
}
