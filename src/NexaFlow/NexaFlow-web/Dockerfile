FROM node:20-alpine AS build
WORKDIR /app
RUN npm install -g pnpm
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile --prod=false
COPY . .
ARG VITE_AUTH_API_URL=http://nexaflow.local/auth
ARG VITE_POS_API_URL=http://nexaflow.local/pos
ARG VITE_BOOK_API_URL=http://nexaflow.local/book
ARG VITE_INSIGHT_API_URL=http://nexaflow.local/insight
ARG VITE_ML_API_URL=http://nexaflow.local/ml
RUN pnpm build

FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
