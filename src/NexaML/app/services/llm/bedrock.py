"""
BedrockLLMService — genera insights en lenguaje natural via AWS Bedrock.

Soporta dos familias de modelos:
  - Anthropic Claude  (anthropic.*)  → Messages API
  - Google Gemma      (google.*)     → Converse API (formato unificado de Bedrock)
  - Fallback genérico → Converse API

El modelo se configura con NEXAML_BEDROCK_MODEL_ID; default: google.gemma-3-4b-it
"""
import json

from app.domain.interfaces import ILLMService
from app.config import settings


class BedrockLLMService(ILLMService):

    async def generate_insight(self, context: dict) -> str:
        import boto3  # lazy — not needed in unit tests

        client = boto3.client("bedrock-runtime", region_name=settings.aws_region)
        model_id = settings.bedrock_model_id
        prompt = self._build_prompt(context)

        if model_id.startswith("anthropic."):
            return self._invoke_claude(client, model_id, prompt)
        else:
            # Converse API — compatible con Gemma, Llama, Mistral, etc.
            return self._invoke_converse(client, model_id, prompt)

    def _invoke_claude(self, client, model_id: str, prompt: str) -> str:
        body = json.dumps({
            "anthropic_version": "bedrock-2023-05-31",
            "max_tokens": 512,
            "messages": [{"role": "user", "content": prompt}],
        })
        response = client.invoke_model(
            modelId=model_id,
            body=body,
            contentType="application/json",
            accept="application/json",
        )
        result = json.loads(response["body"].read())
        return result["content"][0]["text"].strip()

    def _invoke_converse(self, client, model_id: str, prompt: str) -> str:
        response = client.converse(
            modelId=model_id,
            messages=[{"role": "user", "content": [{"text": prompt}]}],
            inferenceConfig={"maxTokens": 512, "temperature": 0.7},
        )
        return response["output"]["message"]["content"][0]["text"].strip()

    def _build_prompt(self, context: dict) -> str:
        return (
            "Eres un analista de negocios para pequeñas empresas (barberías, restaurantes). "
            "Analiza los siguientes datos y genera un insight accionable en español, máximo 3 oraciones. "
            "Sé directo y específico. No uses jerga técnica.\n\n"
            f"Datos del negocio:\n"
            f"- Ticket promedio: ${context.get('avg_ticket', 0):.2f}\n"
            f"- Ventas últimos 30 días: ${context.get('total_revenue_30d', 0):.2f}\n"
            f"- Días con anomalías detectadas: {context.get('anomaly_count', 0)}\n"
            f"- Predicción próximos 7 días: ${context.get('forecast_7d_total', 0):.2f}\n"
            f"- Producto más vendido: {context.get('top_product', 'N/A')}\n\n"
            "Genera el insight:"
        )
