FROM node:20-alpine AS build
WORKDIR /app
RUN npm install -g pnpm
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile --prod=false
COPY . .
ARG VITE_AUTH_API_URL=http://localhost:30081
ARG VITE_POS_API_URL=http://localhost:30082
ARG VITE_BOOK_API_URL=http://localhost:30083
ARG VITE_INSIGHT_API_URL=http://localhost:30084
ARG VITE_ML_API_URL=http://localhost:30085
RUN printf "VITE_AUTH_API_URL=%s\nVITE_POS_API_URL=%s\nVITE_BOOK_API_URL=%s\nVITE_INSIGHT_API_URL=%s\nVITE_ML_API_URL=%s\n" \
    "$VITE_AUTH_API_URL" "$VITE_POS_API_URL" "$VITE_BOOK_API_URL" \
    "$VITE_INSIGHT_API_URL" "$VITE_ML_API_URL" > .env
RUN pnpm build

FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
