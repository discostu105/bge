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
    Statement = [{
      Effect   = "Allow"
      Action   = ["s3:GetObject", "s3:PutObject", "s3:HeadObject"]
      Resource = "${aws_s3_bucket.game_state.arn}/*"
    }]
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
  name                               = var.app_name
  cluster                            = aws_ecs_cluster.app.id
  task_definition                    = aws_ecs_task_definition.app.arn
  desired_count                      = 1
  launch_type                        = "FARGATE"
  availability_zone_rebalancing      = "ENABLED"

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

resource "aws_cloudwatch_metric_alarm" "high_5xx_errors" {
  alarm_name          = "${var.app_name}-high-5xx-errors"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "HTTPCode_Target_5XX_Count"
  namespace           = "AWS/ApplicationELB"
  period              = 300
  statistic           = "Sum"
  threshold           = 10
  alarm_description   = "BGE: High 5xx error rate from targets"
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

resource "aws_cloudwatch_metric_alarm" "high_memory" {
  alarm_name          = "${var.app_name}-high-memory"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 3
  metric_name         = "MemoryUtilization"
  namespace           = "AWS/ECS"
  period              = 300
  statistic           = "Average"
  threshold           = 80
  alarm_description   = "BGE: ECS memory utilization above 80%"
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

resource "aws_cloudwatch_metric_alarm" "high_cpu" {
  alarm_name          = "${var.app_name}-high-cpu"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 3
  metric_name         = "CPUUtilization"
  namespace           = "AWS/ECS"
  period              = 300
  statistic           = "Average"
  threshold           = 80
  alarm_description   = "BGE: ECS CPU utilization above 80%"
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

# --- CloudWatch Dashboard ---

resource "aws_cloudwatch_dashboard" "app" {
  dashboard_name = var.app_name

  lifecycle {
    ignore_changes = [dashboard_body]
  }

  dashboard_body = jsonencode({
    widgets = [
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
            "ECS/ContainerInsights", "RunningTaskCount",
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
  value = "https://${var.aws_region}.console.aws.amazon.com/cloudwatch/home?region=${var.aws_region}#dashboards:name=${var.app_name}"
}

output "sns_alarms_topic_arn" {
  value = aws_sns_topic.alarms.arn
}
