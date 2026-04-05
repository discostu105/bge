terraform {
  required_version = ">= 1.5"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  backend "s3" {
    bucket         = "bge-terraform-state"
    key            = "bge/terraform.tfstate"
    region         = "eu-central-1"
    dynamodb_table = "bge-tf-state-lock"
    encrypt        = true
  }
}

provider "aws" {
  region = var.aws_region
}

variable "aws_region" {
  default = "eu-central-1"
}

variable "app_name" {
  default = "bge"
}

variable "domain_name" {
  description = "Optional domain name for the ALB (leave empty to use ALB DNS)"
  default     = ""
}

variable "acm_certificate_arn" {
  description = "ARN of the ACM certificate for HTTPS listener"
  default     = "arn:aws:acm:eu-central-1:483533431084:certificate/03e96dcf-09bd-46ad-a942-92c8d7467969"
}

variable "github_client_id" {
  sensitive = true
}

variable "github_client_secret" {
  sensitive = true
}

variable "alarm_email" {
  description = "Email address to receive CloudWatch alarm notifications (leave empty to skip subscription)"
  default     = ""
}

variable "monthly_budget_usd" {
  description = "Monthly AWS budget threshold in USD"
  default     = "50"
}

# --- VPC (default) ---

data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }
}

# --- ECR ---

resource "aws_ecr_repository" "app" {
  name                 = var.app_name
  image_tag_mutability = "MUTABLE"
  force_delete         = true

  image_scanning_configuration {
    scan_on_push = true
  }

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

# --- S3 bucket for game state ---

resource "aws_s3_bucket" "game_state" {
  bucket = "${var.app_name}-game-state"

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

resource "aws_s3_bucket_versioning" "game_state" {
  bucket = aws_s3_bucket.game_state.id
  versioning_configuration {
    status = "Enabled"
  }
}

# --- SSM Parameters for secrets ---

resource "aws_ssm_parameter" "github_client_id" {
  name  = "/${var.app_name}/github-client-id"
  type  = "SecureString"
  value = var.github_client_id
}

resource "aws_ssm_parameter" "github_client_secret" {
  name  = "/${var.app_name}/github-client-secret"
  type  = "SecureString"
  value = var.github_client_secret
}

# --- CloudWatch Log Group ---

resource "aws_cloudwatch_log_group" "app" {
  name              = "/ecs/${var.app_name}"
  retention_in_days = 14

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

# --- IAM ---

resource "aws_iam_role" "ecs_task_execution" {
  name = "${var.app_name}-ecs-task-execution"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action    = "sts:AssumeRole"
      Effect    = "Allow"
      Principal = { Service = "ecs-tasks.amazonaws.com" }
    }]
  })

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution" {
  role       = aws_iam_role.ecs_task_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

resource "aws_iam_policy" "ecs_task_execution_ssm" {
  name = "${var.app_name}-ecs-ssm-read-github"
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect   = "Allow"
      Action   = ["ssm:GetParameters", "ssm:GetParameter"]
      Resource = [aws_ssm_parameter.github_client_id.arn, aws_ssm_parameter.github_client_secret.arn]
    }]
  })
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution_ssm" {
  role       = aws_iam_role.ecs_task_execution.name
  policy_arn = aws_iam_policy.ecs_task_execution_ssm.arn
}

resource "aws_iam_role" "ecs_task" {
  name = "${var.app_name}-ecs-task"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action    = "sts:AssumeRole"
      Effect    = "Allow"
      Principal = { Service = "ecs-tasks.amazonaws.com" }
    }]
  })

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

resource "aws_iam_policy" "ecs_task_s3" {
  name = "${var.app_name}-ecs-s3"
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = ["s3:GetObject", "s3:PutObject", "s3:HeadObject"]
        Resource = "${aws_s3_bucket.game_state.arn}/*"
      },
      {
        Effect   = "Allow"
        Action   = ["s3:ListBucket"]
        Resource = aws_s3_bucket.game_state.arn
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "ecs_task_s3" {
  role       = aws_iam_role.ecs_task.name
  policy_arn = aws_iam_policy.ecs_task_s3.arn
}

# --- Security Groups ---

resource "aws_security_group" "alb" {
  name        = "${var.app_name}-alb"
  description = "BGE ALB security group"
  vpc_id      = data.aws_vpc.default.id

  lifecycle {
    ignore_changes = [tags, tags_all]
  }

  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "ecs" {
  name        = "${var.app_name}-ecs"
  description = "BGE ECS security group"
  vpc_id      = data.aws_vpc.default.id

  lifecycle {
    ignore_changes = [tags, tags_all]
  }

  ingress {
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# --- ALB ---

resource "aws_lb" "app" {
  name                       = var.app_name
  internal                   = false
  load_balancer_type         = "application"
  security_groups            = [aws_security_group.alb.id]
  subnets                    = data.aws_subnets.default.ids
  drop_invalid_header_fields = true
  enable_deletion_protection = true

  access_logs {
    bucket  = aws_s3_bucket.alb_logs.id
    prefix  = var.app_name
    enabled = true
  }

  lifecycle {
    ignore_changes = [tags, tags_all]
  }

  depends_on = [aws_s3_bucket_policy.alb_logs]
}

resource "aws_lb_target_group" "app" {
  name        = var.app_name
  port        = 8080
  protocol    = "HTTP"
  vpc_id      = data.aws_vpc.default.id
  target_type = "ip"

  health_check {
    path                = "/health"
    healthy_threshold   = 2
    unhealthy_threshold = 3
    interval            = 30
  }

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

resource "aws_lb_listener" "http" {
  load_balancer_arn = aws_lb.app.arn
  port              = 80
  protocol          = "HTTP"

  default_action {
    type = "redirect"

    redirect {
      protocol    = "HTTPS"
      port        = "443"
      host        = "#{host}"
      path        = "/#{path}"
      query       = "#{query}"
      status_code = "HTTP_301"
    }
  }
}

resource "aws_lb_listener" "https" {
  load_balancer_arn = aws_lb.app.arn
  port              = 443
  protocol          = "HTTPS"
  ssl_policy        = "ELBSecurityPolicy-TLS13-1-2-2021-06"
  certificate_arn   = var.acm_certificate_arn

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.app.arn
  }
}

# --- ECS ---

resource "aws_ecs_cluster" "app" {
  name = var.app_name

  setting {
    name  = "containerInsights"
    value = "disabled"
  }

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

resource "aws_ecs_task_definition" "app" {
  family                   = var.app_name
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = "512"
  memory                   = "1024"
  execution_role_arn       = aws_iam_role.ecs_task_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn

  container_definitions = jsonencode([{
    name      = var.app_name
    image     = "${aws_ecr_repository.app.repository_url}:latest"
    essential = true
    portMappings = [{ containerPort = 8080, protocol = "tcp" }]

    environment = [
      { name = "ASPNETCORE_ENVIRONMENT", value = "Production" },
      { name = "Bge__DevAuth", value = "false" },
      { name = "Bge__S3BucketName", value = aws_s3_bucket.game_state.id },
    ]

    secrets = [
      { name = "GitHub__ClientId", valueFrom = aws_ssm_parameter.github_client_id.arn },
      { name = "GitHub__ClientSecret", valueFrom = aws_ssm_parameter.github_client_secret.arn },
    ]

    logConfiguration = {
      logDriver = "awslogs"
      options = {
        "awslogs-group"         = aws_cloudwatch_log_group.app.name
        "awslogs-region"        = var.aws_region
        "awslogs-stream-prefix" = "ecs"
      }
    }
  }])
}

resource "aws_ecs_service" "app" {
  name                          = var.app_name
  cluster                       = aws_ecs_cluster.app.id
  task_definition               = aws_ecs_task_definition.app.arn
  desired_count                 = 1
  launch_type                   = "FARGATE"
  availability_zone_rebalancing = "ENABLED"

  network_configuration {
    subnets          = data.aws_subnets.default.ids
    security_groups  = [aws_security_group.ecs.id]
    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.app.arn
    container_name   = var.app_name
    container_port   = 8080
  }

  depends_on = [aws_lb_listener.http, aws_lb_listener.https]

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

# --- ALB Access Logs ---

resource "aws_s3_bucket" "alb_logs" {
  bucket = "${var.app_name}-alb-access-logs"

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

resource "aws_s3_bucket_lifecycle_configuration" "alb_logs" {
  bucket = aws_s3_bucket.alb_logs.id

  rule {
    id     = "expire-old-logs"
    status = "Enabled"

    expiration {
      days = 30
    }
  }
}

data "aws_elb_service_account" "main" {}

resource "aws_s3_bucket_policy" "alb_logs" {
  bucket = aws_s3_bucket.alb_logs.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { AWS = data.aws_elb_service_account.main.arn }
      Action    = "s3:PutObject"
      Resource  = "${aws_s3_bucket.alb_logs.arn}/*"
    }]
  })
}

# --- SNS Topic for Alarms ---

resource "aws_sns_topic" "alarms" {
  name = "${var.app_name}-alarms"

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

resource "aws_sns_topic_subscription" "alarms_email" {
  count     = var.alarm_email != "" ? 1 : 0
  topic_arn = aws_sns_topic.alarms.arn
  protocol  = "email"
  endpoint  = var.alarm_email
}

# --- CloudWatch Alarms ---

resource "aws_cloudwatch_metric_alarm" "no_running_tasks" {
  alarm_name          = "${var.app_name}-no-running-tasks"
  comparison_operator = "LessThanThreshold"
  evaluation_periods  = 2
  metric_name         = "RunningTaskCount"
  namespace           = "AWS/ECS"
  period              = 60
  statistic           = "Average"
  threshold           = 1
  alarm_description   = "BGE: No ECS tasks running"
  treat_missing_data  = "breaching"

  dimensions = {
    ClusterName = aws_ecs_cluster.app.name
    ServiceName = aws_ecs_service.app.name
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

resource "aws_cloudwatch_metric_alarm" "no_healthy_hosts" {
  alarm_name          = "${var.app_name}-no-healthy-hosts"
  comparison_operator = "LessThanThreshold"
  evaluation_periods  = 2
  metric_name         = "HealthyHostCount"
  namespace           = "AWS/ApplicationELB"
  period              = 60
  statistic           = "Minimum"
  threshold           = 1
  alarm_description   = "BGE: No healthy targets behind ALB"
  treat_missing_data  = "breaching"

  dimensions = {
    TargetGroup  = aws_lb_target_group.app.arn_suffix
    LoadBalancer = aws_lb.app.arn_suffix
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

# 5xx error rate alarm: alerts when 5xx errors exceed 1% of total requests over a 5-min window.
# Uses metric math to compute the rate; treats missing data as not breaching (low-traffic periods).
resource "aws_cloudwatch_metric_alarm" "high_5xx_rate" {
  alarm_name          = "${var.app_name}-high-5xx-rate"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  threshold           = 1
  alarm_description   = "BGE: 5xx error rate > 1% over 5-min window"
  treat_missing_data  = "notBreaching"

  metric_query {
    id          = "error_rate"
    expression  = "100 * errors / MAX([errors, requests])"
    label       = "5xx Error Rate (%)"
    return_data = true
  }

  metric_query {
    id = "errors"
    metric {
      metric_name = "HTTPCode_Target_5XX_Count"
      namespace   = "AWS/ApplicationELB"
      period      = 300
      stat        = "Sum"
      dimensions  = { LoadBalancer = aws_lb.app.arn_suffix }
    }
  }

  metric_query {
    id = "requests"
    metric {
      metric_name = "RequestCount"
      namespace   = "AWS/ApplicationELB"
      period      = 300
      stat        = "Sum"
      dimensions  = { LoadBalancer = aws_lb.app.arn_suffix }
    }
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

# p99 latency alarm: 2 consecutive 5-min periods above 2s triggers.
resource "aws_cloudwatch_metric_alarm" "high_p99_latency" {
  alarm_name          = "${var.app_name}-high-p99-latency"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "TargetResponseTime"
  namespace           = "AWS/ApplicationELB"
  period              = 300
  extended_statistic  = "p99"
  threshold           = 2
  alarm_description   = "BGE: p99 API latency > 2s over 5-min window"
  treat_missing_data  = "notBreaching"

  dimensions = {
    LoadBalancer = aws_lb.app.arn_suffix
  }

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

# ECS CPU: 2 consecutive 5-min periods (= 10 min sustained) above 80%.
resource "aws_cloudwatch_metric_alarm" "high_cpu" {
  alarm_name          = "${var.app_name}-high-cpu"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "CPUUtilization"
  namespace           = "AWS/ECS"
  period              = 300
  statistic           = "Average"
  threshold           = 80
  alarm_description   = "BGE: ECS CPU utilization above 80% for 10 min"
  treat_missing_data  = "notBreaching"

  dimensions = {
    ClusterName = aws_ecs_cluster.app.name
    ServiceName = aws_ecs_service.app.name
  }

  alarm_actions = [aws_sns_topic.alarms.arn]

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

# ECS Memory: alert at 85%.
resource "aws_cloudwatch_metric_alarm" "high_memory" {
  alarm_name          = "${var.app_name}-high-memory"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 3
  metric_name         = "MemoryUtilization"
  namespace           = "AWS/ECS"
  period              = 300
  statistic           = "Average"
  threshold           = 85
  alarm_description   = "BGE: ECS memory utilization above 85%"
  treat_missing_data  = "notBreaching"

  dimensions = {
    ClusterName = aws_ecs_cluster.app.name
    ServiceName = aws_ecs_service.app.name
  }

  alarm_actions = [aws_sns_topic.alarms.arn]

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

# --- Application Log Metric Filter: Unhandled Exceptions ---

resource "aws_cloudwatch_log_metric_filter" "exceptions" {
  name           = "${var.app_name}-unhandled-exceptions"
  log_group_name = aws_cloudwatch_log_group.app.name
  # ASP.NET Core logs unhandled exceptions with these phrases.
  pattern = "?\"Unhandled exception\" ?\"An unhandled exception has occurred\""

  metric_transformation {
    name          = "UnhandledExceptions"
    namespace     = "BGE/Application"
    value         = "1"
    default_value = "0"
  }
}

resource "aws_cloudwatch_metric_alarm" "unhandled_exceptions" {
  alarm_name          = "${var.app_name}-unhandled-exceptions"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = "UnhandledExceptions"
  namespace           = "BGE/Application"
  period              = 300
  statistic           = "Sum"
  threshold           = 0
  alarm_description   = "BGE: One or more unhandled exceptions detected in application logs"
  treat_missing_data  = "notBreaching"

  alarm_actions = [aws_sns_topic.alarms.arn]

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

# --- Application Log Metric Filter: Game Tick Failures ---

resource "aws_cloudwatch_log_metric_filter" "tick_failures" {
  name           = "${var.app_name}-tick-failures"
  log_group_name = aws_cloudwatch_log_group.app.name
  # Matches structured log lines emitted by GameTickTimerService on tick errors,
  # as well as the critical guard log from GameTickEngine when a tick hangs.
  pattern = "?GameTickFailure ?\"CheckAllTicks exceeded\""

  metric_transformation {
    name          = "GameTickFailures"
    namespace     = "BGE/Application"
    value         = "1"
    default_value = "0"
  }
}

# Alert after 3 consecutive 1-minute periods each recording at least one tick failure.
resource "aws_cloudwatch_metric_alarm" "tick_failures" {
  alarm_name          = "${var.app_name}-tick-failures"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 3
  metric_name         = "GameTickFailures"
  namespace           = "BGE/Application"
  period              = 60
  statistic           = "Sum"
  threshold           = 0
  alarm_description   = "BGE: Game tick failures detected for 3 consecutive minutes"
  treat_missing_data  = "notBreaching"

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

# --- Application Log Metric Filter: Container Restarts ---

# Matches the ASP.NET Core hosted-service shutdown log emitted when the container stops.
# Each occurrence indicates the game server process exited (and ECS will restart it).
resource "aws_cloudwatch_log_metric_filter" "container_restarts" {
  name           = "${var.app_name}-container-restarts"
  log_group_name = aws_cloudwatch_log_group.app.name
  pattern        = "\"Timed Hosted Service is stopping\""

  metric_transformation {
    name          = "ContainerRestarts"
    namespace     = "BGE/Application"
    value         = "1"
    default_value = "0"
  }
}

resource "aws_cloudwatch_metric_alarm" "container_restarts" {
  alarm_name          = "${var.app_name}-container-restarts"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = "ContainerRestarts"
  namespace           = "BGE/Application"
  period              = 300
  statistic           = "Sum"
  threshold           = 0
  alarm_description   = "BGE: Container/hosted-service restart detected"
  treat_missing_data  = "notBreaching"

  alarm_actions = [aws_sns_topic.alarms.arn]
  ok_actions    = [aws_sns_topic.alarms.arn]

  lifecycle {
    ignore_changes = [tags, tags_all]
  }
}

# --- AWS Budget Alert ---

resource "aws_budgets_budget" "monthly" {
  name         = "${var.app_name}-monthly"
  budget_type  = "COST"
  limit_amount = var.monthly_budget_usd
  limit_unit   = "USD"
  time_unit    = "MONTHLY"

  notification {
    comparison_operator        = "GREATER_THAN"
    threshold                  = 80
    threshold_type             = "PERCENTAGE"
    notification_type          = "ACTUAL"
    subscriber_email_addresses = var.alarm_email != "" ? [var.alarm_email] : []
    subscriber_sns_topic_arns  = [aws_sns_topic.alarms.arn]
  }

  notification {
    comparison_operator        = "GREATER_THAN"
    threshold                  = 100
    threshold_type             = "PERCENTAGE"
    notification_type          = "ACTUAL"
    subscriber_email_addresses = var.alarm_email != "" ? [var.alarm_email] : []
    subscriber_sns_topic_arns  = [aws_sns_topic.alarms.arn]
  }
}

# --- CloudWatch Dashboard ---

resource "aws_cloudwatch_dashboard" "production" {
  dashboard_name = "BGE-Production"

  dashboard_body = jsonencode({
    widgets = [
      # Row 0: ECS / ALB infrastructure overview
      {
        type   = "metric"
        x      = 0
        y      = 0
        width  = 6
        height = 6
        properties = {
          title  = "ECS Running Task Count"
          region = var.aws_region
          metrics = [[
            "AWS/ECS", "RunningTaskCount",
            "ClusterName", aws_ecs_cluster.app.name,
            "ServiceName", aws_ecs_service.app.name
          ]]
          stat   = "Average"
          period = 60
          view   = "timeSeries"
          yAxis  = { left = { min = 0 } }
        }
      },
      {
        type   = "metric"
        x      = 6
        y      = 0
        width  = 6
        height = 6
        properties = {
          title  = "ECS CPU & Memory Utilization (%)"
          region = var.aws_region
          metrics = [
            ["AWS/ECS", "CPUUtilization", "ClusterName", aws_ecs_cluster.app.name, "ServiceName", aws_ecs_service.app.name],
            ["AWS/ECS", "MemoryUtilization", "ClusterName", aws_ecs_cluster.app.name, "ServiceName", aws_ecs_service.app.name]
          ]
          stat   = "Average"
          period = 300
          view   = "timeSeries"
          yAxis  = { left = { min = 0, max = 100 } }
        }
      },
      {
        type   = "metric"
        x      = 12
        y      = 0
        width  = 6
        height = 6
        properties = {
          title  = "ALB Request Count"
          region = var.aws_region
          metrics = [[
            "AWS/ApplicationELB", "RequestCount",
            "LoadBalancer", aws_lb.app.arn_suffix
          ]]
          stat   = "Sum"
          period = 60
          view   = "timeSeries"
        }
      },
      {
        type   = "metric"
        x      = 18
        y      = 0
        width  = 6
        height = 6
        properties = {
          title  = "ALB 5xx / 4xx Error Count"
          region = var.aws_region
          metrics = [
            ["AWS/ApplicationELB", "HTTPCode_Target_5XX_Count", "LoadBalancer", aws_lb.app.arn_suffix],
            ["AWS/ApplicationELB", "HTTPCode_Target_4XX_Count", "LoadBalancer", aws_lb.app.arn_suffix]
          ]
          stat   = "Sum"
          period = 60
          view   = "timeSeries"
        }
      },
      # Row 6: Latency & ALB health
      {
        type   = "metric"
        x      = 0
        y      = 6
        width  = 12
        height = 6
        properties = {
          title  = "ALB Target Response Time (p50 / p95 / p99)"
          region = var.aws_region
          metrics = [
            ["AWS/ApplicationELB", "TargetResponseTime", "LoadBalancer", aws_lb.app.arn_suffix, { stat = "p50", label = "p50" }],
            ["AWS/ApplicationELB", "TargetResponseTime", "LoadBalancer", aws_lb.app.arn_suffix, { stat = "p95", label = "p95" }],
            ["AWS/ApplicationELB", "TargetResponseTime", "LoadBalancer", aws_lb.app.arn_suffix, { stat = "p99", label = "p99" }]
          ]
          period = 60
          view   = "timeSeries"
          yAxis  = { left = { min = 0 } }
        }
      },
      {
        type   = "metric"
        x      = 12
        y      = 6
        width  = 12
        height = 6
        properties = {
          title  = "ALB Target Health"
          region = var.aws_region
          metrics = [
            ["AWS/ApplicationELB", "HealthyHostCount", "TargetGroup", aws_lb_target_group.app.arn_suffix, "LoadBalancer", aws_lb.app.arn_suffix],
            ["AWS/ApplicationELB", "UnHealthyHostCount", "TargetGroup", aws_lb_target_group.app.arn_suffix, "LoadBalancer", aws_lb.app.arn_suffix]
          ]
          stat   = "Average"
          period = 60
          view   = "timeSeries"
          yAxis  = { left = { min = 0 } }
        }
      },
      # Row 12: Application-level metrics
      {
        type   = "metric"
        x      = 0
        y      = 12
        width  = 8
        height = 6
        properties = {
          title  = "Unhandled Application Exceptions"
          region = var.aws_region
          metrics = [[
            "BGE/Application", "UnhandledExceptions"
          ]]
          stat   = "Sum"
          period = 300
          view   = "timeSeries"
          yAxis  = { left = { min = 0 } }
        }
      },
      {
        type   = "metric"
        x      = 8
        y      = 12
        width  = 8
        height = 6
        properties = {
          title  = "Game Tick Failures"
          region = var.aws_region
          metrics = [[
            "BGE/Application", "GameTickFailures"
          ]]
          stat   = "Sum"
          period = 60
          view   = "timeSeries"
          yAxis  = { left = { min = 0 } }
        }
      },
      {
        type   = "metric"
        x      = 16
        y      = 12
        width  = 8
        height = 6
        properties = {
          title  = "Container Restarts"
          region = var.aws_region
          metrics = [[
            "BGE/Application", "ContainerRestarts"
          ]]
          stat   = "Sum"
          period = 300
          view   = "timeSeries"
          yAxis  = { left = { min = 0 } }
        }
      },
      # Row 18: Game tick duration (Logs Insights)
      {
        type = "log"
        x    = 0
        y    = 18
        width  = 24
        height = 6
        properties = {
          title   = "Game Tick Duration (ms) — last 3 hours"
          region  = var.aws_region
          view    = "timeSeries"
          stacked = false
          query   = "SOURCE '/ecs/${var.app_name}' | fields @timestamp, TickDurationMs, PlayerCount, GameId | filter ispresent(TickDurationMs) | stats avg(TickDurationMs) as AvgMs, max(TickDurationMs) as MaxMs, avg(PlayerCount) as AvgPlayers by bin(1m)"
          period  = 60
        }
      }
    ]
  })
}

# --- Outputs ---

output "alb_dns_name" {
  value = aws_lb.app.dns_name
}

output "ecr_repository_url" {
  value = aws_ecr_repository.app.repository_url
}

output "cloudwatch_dashboard_url" {
  value = "https://${var.aws_region}.console.aws.amazon.com/cloudwatch/home?region=${var.aws_region}#dashboards:name=BGE-Production"
}

output "sns_alarms_topic_arn" {
  value = aws_sns_topic.alarms.arn
}
