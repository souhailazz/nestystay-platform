export interface SystemHealthStatus {
  service: string;
  status: "Healthy" | "Degraded" | "Unhealthy";
  database: string;
  openApi: string;
}

export interface AdminUserItem {
  id: string;
  email: string;
  displayName: string;
  role: "Traveler" | "Host" | "Admin" | "Support";
  status: "Active" | "Suspended" | "Flagged";
  identityStatus: string;
  createdAt: string;
}

export interface AuditTrailLog {
  id: string;
  timestamp: string;
  actorEmail: string;
  action: string;
  resourceType: string;
  resourceId: string;
  details: string;
}
