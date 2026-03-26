export const useTenant = () => {
  const tenantId = localStorage.getItem('tenantId') ?? '';
  return { tenantId };
};
