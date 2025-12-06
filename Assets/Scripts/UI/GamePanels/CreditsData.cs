using UnityEngine;
using System;
using System.Collections.Generic;

public enum ContributorType
{
    CoreTeam,
    Freelancer,
    Backer,
    SpecialThanks,
    Tooling
}

public enum Department
{
    Management,
    Programming,
    Design,
    Art,
    Animation,
    Audio,
    QA,
    Marketing,
    Community
}

public enum Seniority
{
    Director = 0,
    Lead = 1,
    Senior = 2,
    Mid = 3,
    Junior = 4,
    Intern = 5,
    Contributor = 6
}

[Serializable]
public class Role
{
    public Department department = Department.Programming;
    public Seniority seniority = Seniority.Mid;
    
    [Tooltip("Leave empty to use auto-generated title")]
    public string customRoleTitle = "";
    
    public string GetFormattedTitle()
    {
        if (!string.IsNullOrEmpty(customRoleTitle))
        {
            return customRoleTitle;
        }
        
        string seniorityStr = GetSeniorityString();
        string departmentStr = GetDepartmentString();
        
        if (string.IsNullOrEmpty(seniorityStr))
        {
            return departmentStr;
        }
        
        return $"{seniorityStr} {departmentStr}";
    }
    
    private string GetSeniorityString()
    {
        switch (seniority)
        {
            case Seniority.Director:
                return "Director";
            case Seniority.Lead:
                return "Lead";
            case Seniority.Senior:
                return "Senior";
            case Seniority.Mid:
                return "";
            case Seniority.Junior:
                return "Junior";
            case Seniority.Intern:
                return "Intern";
            case Seniority.Contributor:
                return "Contributor";
            default:
                return "";
        }
    }
    
    private string GetDepartmentString()
    {
        switch (department)
        {
            case Department.Management:
                return "Management";
            case Department.Programming:
                return "Programmer";
            case Department.Design:
                return "Designer";
            case Department.Art:
                return "Artist";
            case Department.Animation:
                return "Animator";
            case Department.Audio:
                return "Audio Designer";
            case Department.QA:
                return "QA Tester";
            case Department.Marketing:
                return "Marketing";
            case Department.Community:
                return "Community Manager";
            default:
                return "";
        }
    }
    
    public int GetSortPriority()
    {
        int deptOrder = (int)department * 1000;
        int seniorityOrder = (int)seniority * 100;
        return deptOrder + seniorityOrder;
    }
}

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
