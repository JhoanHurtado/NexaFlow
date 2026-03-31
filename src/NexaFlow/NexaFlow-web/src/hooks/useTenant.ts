export const useTenant = () => {
  const tenantId = localStorage.getItem('tenantId') ?? '';
  const role     = localStorage.getItem('role') ?? '';
  const userName = localStorage.getItem('userName') ?? '';
  return { tenantId, role, userName };
};
