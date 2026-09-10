import { UsersApi } from './users.api';

export const userQueryKeys = {
  all: ['users'] as const,
  list: ['users', 'list'] as const,
  invitation: ['invitation'] as const,
};
export const usersQueryOptions = (api: UsersApi) => ({
  queryKey: userQueryKeys.list,
  queryFn: () => api.list(),
});
